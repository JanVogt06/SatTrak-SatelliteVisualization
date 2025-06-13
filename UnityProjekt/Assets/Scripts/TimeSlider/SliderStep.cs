using System;

namespace TimeSlider
{
    public class SliderStep
    {
        public int Min;
        public int Max;
        public Types Type;

        public SliderStep(int min, int max, Types type)
        {
            Min = min;
            Max = max;
            Type = type;
        }

        public string Name => Type.ToString();

        public string ToMinDateString(DateTime currenTime)
        {
            var date = ToDate(currenTime, -(Max / 2));
            return date.ToString("HH:mm:ss dd.MM.yyyy");
        }

        public string ToMaxDateString(DateTime currenTime)
        {
            var date = ToDate(currenTime, Max / 2);
            return date.ToString("HH:mm:ss dd.MM.yyyy");
        }

        public DateTime ToMinDate(DateTime currenTime)
        {
            return ToDate(currenTime, -(Max / 2));
        }

        public DateTime ToMaxDate(DateTime currenTime)
        {
            return ToDate(currenTime, Max / 2);
        }

        public DateTime ToDate(DateTime currentTime, int value) => Type switch
        {
            Types.Month => currentTime.AddMonths(value),
            Types.Day => currentTime.AddMonths(value),
            Types.Hour => currentTime.AddHours(value),
            Types.Minute => currentTime.AddMinutes(value),
            Types.Second => currentTime.AddSeconds(value),
            _ => throw new ArgumentOutOfRangeException()
        };

        public DateTime SetDate(DateTime currentTime, int value) => Type switch
        {
            Types.Month => new DateTime(currentTime.Year, value, currentTime.Day, currentTime.Hour, currentTime.Minute, currentTime.Second, DateTimeKind.Local),
            Types.Day => new DateTime(currentTime.Year, currentTime.Month, value, currentTime.Hour, currentTime.Minute, currentTime.Second, DateTimeKind.Local),
            Types.Hour => new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, value, currentTime.Minute, currentTime.Second, DateTimeKind.Local),
            Types.Minute => new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, value, currentTime.Second, DateTimeKind.Local),
            Types.Second => new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, value, DateTimeKind.Local),
            _ => throw new ArgumentOutOfRangeException()
        };

        public enum Types
        {
            Month,
            Day,
            Hour,
            Minute,
            Second
        }

        public float ToSliderValue(DateTime currentTime)
            => Type switch
            {
                Types.Month => currentTime.Month,
                Types.Day => currentTime.Day,
                Types.Hour => currentTime.Hour,
                Types.Minute => currentTime.Minute,
                Types.Second => currentTime.Second,
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}