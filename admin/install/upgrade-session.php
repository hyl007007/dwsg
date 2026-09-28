<?php
// 旧安装更新代码后，在部署目录执行：php install/upgrade-session.php
if (PHP_SAPI !== 'cli') { http_response_code(403); exit; }
require __DIR__.'/../include/config.php';
if (!preg_match('/^[A-Za-z_][A-Za-z0-9_]*$/D', DB_PRE)) exit("Invalid table prefix\n");
mysqli_report(MYSQLI_REPORT_ERROR | MYSQLI_REPORT_STRICT);
$db = new mysqli(DB_HOST, DB_USER, DB_PASSWD, DB_NAME, (int)DB_PORT);
$db->set_charset('utf8mb4');
$table = DB_PRE.'heartbeat';
$column = $db->query("SHOW COLUMNS FROM `$table` LIKE 'session_token_hash'");
if (!$column->num_rows) $db->query("ALTER TABLE `$table` ADD session_token_hash CHAR(64) DEFAULT NULL");
// 云数据先经过接口日志；只升级用户表会让四字节字符仍在日志写入时失败。
foreach (['user','apilog','applog'] as $suffix) {
    $db->query('ALTER TABLE `'.DB_PRE.$suffix.'` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci');
}
$db->close();
echo "Session schema ready; legacy sessions require a new login.\n";
