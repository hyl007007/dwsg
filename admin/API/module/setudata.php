<?php
if($myurl != $_SERVER['SERVER_NAME']){ //拦截未通过认证的调用
    header("content-type:text/html; charset=utf-8");
    $udata = array('code'=>201,'msg'=>'null');
    $jdata = json_encode($udata);
    die($jdata);
}
    $cztype = '存云数据';
    $czzt = '存储失败';

    require 'profiler.php';

    $udata = $d['udata'] ?? '';
    if (!is_string($udata) || strlen($udata) > 65535 || !mb_check_encoding($udata, 'UTF-8'))
        out(201, '云数据格式错误或超过 65535 字节。', $app_res);
    $res = DB::executePrepared('UPDATE `'.DB_PRE.'user` SET data2=? WHERE id=? AND appid=?', [$udata, $token['uid'], $appid]);
    if (!$res) out(201, '云数据存储失败。', $app_res);
    insert_userlog($user_o['id'],$appid,$alid,$g_date,$ver,$mac,$ip,$clientid,'[存云数据] > 存储成功.');
    out(200, '用户数据存储至云端成功。', $app_res);

?>
