<?php
// 会话只接受登录时发出的随机凭据；数字 ID 与设备 ID 不构成授权。
function require_api_session() {
    global $d, $appid, $account, $clientid, $app_res;
    $session_id = filter_var($d['tokenid'] ?? null, FILTER_VALIDATE_INT, ['options'=>['min_range'=>1]]);
    $credential = $d['session_token'] ?? '';
    if (!$session_id || !is_string($credential) || !preg_match('/^[a-f0-9]{64}$/D', $credential) || $clientid === '')
        out(201, '会话无效，请重新登录。', $app_res);
    $owner = DB::table('user')->where(['user'=>$account,'appid'=>$appid])->find();
    if (!$owner) out(201, '会话无效，请重新登录。', $app_res);
    $session = DB::table('heartbeat')->where(['id'=>$session_id,'uid'=>$owner['id'],'appid'=>$appid,'clientid'=>$clientid])->find();
    if (!$session || empty($session['session_token_hash']) ||
        !hash_equals($session['session_token_hash'], hash('sha256', $credential)))
        out(201, '会话无效，请重新登录。', $app_res);
    return $session;
}
