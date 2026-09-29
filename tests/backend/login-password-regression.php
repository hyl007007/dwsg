<?php
// Run against the real api.php with an isolated database initialized from install.sql.
// DWSG_LOGIN_TEST_CONFIG: temporary config.php; DWSG_LOGIN_TEST_URL: loopback api.php URL.
if (PHP_SAPI !== 'cli') { http_response_code(404); exit; }
mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT);
$config = getenv('DWSG_LOGIN_TEST_CONFIG');
$url = getenv('DWSG_LOGIN_TEST_URL');
if (!$config || !is_file($config) || !preg_match('#^http://127\.0\.0\.1:[0-9]+/api\.php$#D', $url ?: '')) {
    fwrite(STDERR, "Set the temporary config and loopback API URL.\n");
    exit(2);
}
require $config;
if (DB_HOST !== '127.0.0.1' || !preg_match('/^dwsg_login_regression_[a-f0-9]{32}$/D', DB_NAME) || DB_PRE !== 'ty_') {
    fwrite(STDERR, "Only the dedicated regression database is allowed.\n");
    exit(2);
}
$checks = [];
function check($name, $passed, $details = []) {
    global $checks;
    $checks[] = ['case'=>$name, 'passed'=>(bool)$passed] + $details;
}
function request_api($appid, $fields) {
    global $url;
    $context = stream_context_create(['http'=>[
        'method'=>'POST', 'header'=>"Content-Type: application/x-www-form-urlencoded\r\n",
        'content'=>http_build_query($fields), 'timeout'=>10, 'ignore_errors'=>true
    ]]);
    $body = file_get_contents($url.'?appid='.$appid, false, $context);
    if ($body === false) throw new RuntimeException('API request failed');
    $reply = json_decode($body, true, 512, JSON_THROW_ON_ERROR);
    if (!isset($reply['data']['code'], $reply['data']['result'])) throw new RuntimeException('Invalid API response');
    return $reply['data'];
}
function scalar_query($sql, $values = []) {
    global $db;
    $statement = $db->prepare($sql);
    if ($values) $statement->bind_param(str_repeat('s', count($values)), ...$values);
    $statement->execute();
    $value = $statement->get_result()->fetch_row()[0];
    $statement->close();
    return $value;
}
function login_case($name, $user, $password, $expectedCode, $appid = 1) {
    $client = 'reg_'.bin2hex(random_bytes(8));
    $fields = ['action'=>'login', 'user'=>$user, 'clientid'=>$client, 'mac'=>'regression-device',
        'ip'=>'127.0.0.1', 'ver'=>'regression', 'md5'=>'', 'uuid'=>$client];
    if ($password !== null) $fields['pwd'] = $password;
    $reply = request_api($appid, $fields);
    $sessions = (int)scalar_query('SELECT COUNT(*) FROM ty_heartbeat WHERE clientid=?', [$client]);
    $passed = $reply['code'] === $expectedCode && $sessions === ($expectedCode === 200 ? 1 : 0);
    if ($expectedCode === 200 && $reply['code'] === 200) {
        $result = $reply['result'];
        $stored = scalar_query('SELECT session_token_hash FROM ty_heartbeat WHERE id=? AND clientid=?', [$result['tokenid'], $client]);
        $passed = $passed && strlen($result['session_token'] ?? '') === 64 &&
            hash_equals($stored ?: '', hash('sha256', $result['session_token'] ?? ''));
    } elseif ($expectedCode !== 200 && !is_array($password)) {
        $passed = $passed && ($reply['result']['ret_info'] ?? '') === '登录失败，账号或密码错误。';
    }
    check($name, $passed, ['expectedCode'=>$expectedCode, 'actualCode'=>$reply['code'], 'heartbeatCount'=>$sessions]);
    return [$fields, $reply];
}
try {
    $db = new mysqli(DB_HOST, DB_USER, DB_PASSWD, DB_NAME, (int)DB_PORT);
    $db->set_charset('utf8mb4');
    // These tables belong exclusively to this regression database, never dwsg_local.
    foreach (['heartbeat','apilog','applog','cardlog','card','user','app','api'] as $table) $db->query("DELETE FROM ty_$table");
    foreach (['login','heartbeat','logout'] as $action) {
        $db->query("INSERT INTO ty_api (name,ec_api,in_api,addtime) VALUES ('$action','$action','$action',NOW())");
    }
    $db->query("INSERT INTO ty_app (id,name,appkey,orcheck,dl_type,dl_type2) VALUES
        (1,'password regression','',3,0,1),(2,'card regression','',3,1,1)");
    $db->query("INSERT INTO ty_user (id,appid,user,pwd,endtime,point,addtime,data) VALUES
        (101,1,'reg_default',MD5('123456'),'2030-01-01',100,NOW(),''),
        (102,1,'reg_zero',MD5('0'),'2030-01-01',100,NOW(),''),
        (103,1,'reg_other',MD5('another-password'),'2030-01-01',100,NOW(),''),
        (104,2,'reg_existing_card',MD5('unused-card-password'),'2030-01-01',100,NOW(),'')");
    $db->query("INSERT INTO ty_card (name,appid,card,rgtime,rgpoint,addtime,data,bz) VALUES
        ('regression card',2,'REGRESSION_NEW_CARD',60,100,NOW(),'','')");
    login_case('missing password rejected without session', 'reg_default', null, 201);
    login_case('empty password rejected without session', 'reg_default', '', 201);
    [$proof, $login] = login_case('correct password issues hashed session', 'reg_default', '123456', 200);
    login_case('wrong password rejected without session', 'reg_default', 'incorrect', 201);
    login_case('string zero password accepted', 'reg_zero', '0', 200);
    login_case('wrong password for zero account rejected', 'reg_zero', '123456', 201);
    login_case('array password rejected without session', 'reg_default', ['123456'], 201);
    login_case('array with empty password rejected without session', 'reg_default', [''], 201);
    login_case('existing card account accepts no password', 'reg_existing_card', null, 200, 2);
    login_case('new card activation accepts no password', 'REGRESSION_NEW_CARD', null, 200, 2);
    check('new card consumed and account created once',
        (int)scalar_query("SELECT COUNT(*) FROM ty_card WHERE card='REGRESSION_NEW_CARD'") === 0 &&
        (int)scalar_query("SELECT COUNT(*) FROM ty_user WHERE appid=2 AND user='REGRESSION_NEW_CARD'") === 1);

    unset($proof['pwd']);
    $proof['action'] = 'heartbeat';
    $proof['tokenid'] = $login['result']['tokenid'];
    $proof['session_token'] = $login['result']['session_token'];
    $reply = request_api(1, $proof);
    check('valid session heartbeat succeeds', $reply['code'] === 200);
    $badToken = ($proof['session_token'][0] === 'a' ? 'b' : 'a').substr($proof['session_token'], 1);
    foreach ([
        'missing session credential'=>['session_token'=>''],
        'wrong session credential'=>['session_token'=>$badToken],
        'wrong session id'=>['tokenid'=>0],
        'cross-account heartbeat'=>['user'=>'reg_other'],
        'cross-client heartbeat'=>['clientid'=>'reg_wrong_client']
    ] as $name=>$changes) {
        $reply = request_api(1, array_replace($proof, $changes));
        check($name, $reply['code'] === 201 && (int)scalar_query('SELECT COUNT(*) FROM ty_heartbeat WHERE id=?', [$proof['tokenid']]) === 1);
    }
    $reply = request_api(2, $proof);
    check('cross-application heartbeat rejected', $reply['code'] === 201 &&
        (int)scalar_query('SELECT COUNT(*) FROM ty_heartbeat WHERE id=?', [$proof['tokenid']]) === 1);
    $reply = request_api(1, array_replace($proof, ['action'=>'logout', 'session_token'=>$badToken]));
    check('unauthorized logout preserves session', $reply['code'] === 201 &&
        (int)scalar_query('SELECT COUNT(*) FROM ty_heartbeat WHERE id=?', [$proof['tokenid']]) === 1);
    $reply = request_api(1, array_replace($proof, ['action'=>'logout']));
    check('authorized logout revokes session', $reply['code'] === 200 &&
        (int)scalar_query('SELECT COUNT(*) FROM ty_heartbeat WHERE id=?', [$proof['tokenid']]) === 0);
    $reply = request_api(1, $proof);
    check('revoked heartbeat rejected', $reply['code'] === 201);
    $rows = $db->query('SELECT data FROM ty_apilog');
    $redacted = true;
    while ($row = $rows->fetch_assoc()) {
        $logged = json_decode($row['data'], true, 512, JSON_THROW_ON_ERROR);
        $redacted = $redacted && !isset($logged['pwd']) && !isset($logged['session_token']);
    }
    check('API logs omit passwords and session credentials', $redacted && $rows->num_rows > 0);
    $failed = count(array_filter($checks, fn($check)=>!$check['passed']));
    echo json_encode(['passed'=>count($checks)-$failed,'failed'=>$failed,'checks'=>$checks], JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE)."\n";
    exit($failed ? 1 : 0);
} catch (Throwable $error) {
    fwrite(STDERR, 'Regression could not complete: '.get_class($error)."\n");
    exit(2);
}
