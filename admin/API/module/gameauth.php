<?php
$cztype = '游戏会话验证';
$czzt = '验证失败';
require 'profiler.php';
$expires = strtotime($token['hbtime']) + (int)$app_res['xttime'];
if ($app_res['orcheck'] == 1) $expires = min($expires, strtotime($user['endtime']));
out(200, [
    'accountId'=>'php:'.(int)$appid.':'.(int)$user['id'],
    'appid'=>(int)$appid,
    'sessionExpiresUtcMs'=>$expires * 1000,
    'ret_info'=>'游戏会话验证成功。'
], $app_res);
