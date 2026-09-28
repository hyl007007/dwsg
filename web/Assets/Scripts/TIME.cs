using System;

public static class TIME
{
	private static DateTime timeStampStartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static long getTime()
	{
		return DateTimeToTimeStamp(DateTime.Now);
	}

	public static long getTimems()
	{
		return DateTimeToLongTimeStamp(DateTime.Now);
	}

	public static long DateTimeToTimeStamp(DateTime dateTime)
	{
		return (long)(dateTime.ToUniversalTime() - timeStampStartTime).TotalSeconds;
	}

	public static long DateTimeToLongTimeStamp(DateTime dateTime)
	{
		return (long)(dateTime.ToUniversalTime() - timeStampStartTime).TotalMilliseconds;
	}

	public static DateTime TimeStampToDateTime(long timeStamp)
	{
		return timeStampStartTime.AddSeconds(timeStamp).ToLocalTime();
	}

	public static DateTime LongTimeStampToDateTime(long longTimeStamp)
	{
		return timeStampStartTime.AddMilliseconds(longTimeStamp).ToLocalTime();
	}

	public static string ToTimeFormat(long time)
	{
		int num = (int)time;
		int num2 = num / 3600;
		int num3 = num % 3600 / 60;
		num = num % 3600 % 60;
		return $"{num2:D2}:{num3:D2}:{num:D2}";
	}

	public static string 转时间格式2()
	{
		return TimeStampToDateTime(getTime()).ToString("HH:mm");
	}

	public static string 转时间格式(long time)
	{
		return TimeStampToDateTime(time).ToString();
	}
}
