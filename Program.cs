using System.Globalization;

namespace LogAnyliser
{
    internal class Program
    {
        private const string FILE_LOG = "--file-log";
        private const string FILE_OUTPUT = "--file-output";
        private const string ADDRESS_START = "--address-start";
        private const string ADDRESS_MASK = "--address-mask";
        private const string TIME_START = "--time-start";
        private const string TIME_END = "--time-end";
        private const string DATETIME_LOG_FORMAT = "yyyy-MM-dd HH:mm:ss";
        private const string DATETIME_ARGS_FORMAT = "dd.MM.yyyy";
        private static Dictionary<string, string> _argumentValues = new Dictionary<string, string>() { { FILE_LOG, "" }, { FILE_OUTPUT, "" }, { ADDRESS_START, "" }, { ADDRESS_MASK, "" }, { TIME_START, "" }, { TIME_END, "" } };

        static void Main(string[] args)
        {
            GetArgumentsValues(args);            
            if (string.IsNullOrEmpty(_argumentValues[FILE_LOG]) || string.IsNullOrEmpty(_argumentValues[FILE_OUTPUT])) 
            {
                Console.WriteLine("Отсутствует один или несколько обязательных параметров");
                return;
            }

            List<AccessLog> accessLogs = new List<AccessLog>();
            try
            {
                accessLogs = GetAccessLogs(_argumentValues[FILE_LOG]);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Ошибка при получение логов из файла. {ex.Message}") ;
                return;
            }

            DateTime timeStart, timeEnd;
            try
            {
                (timeStart, timeEnd) = GetTimeInterval();
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Ошибка при получении временного интервала из аргументов командной строки. {ex.Message}");
                return;
            }

            string addressStart = _argumentValues[ADDRESS_START], addressEnd = "9999999999999999";
            if (!string.IsNullOrEmpty(addressStart) && !string.IsNullOrEmpty(_argumentValues[ADDRESS_MASK]))
            {
                addressEnd = _argumentValues[ADDRESS_MASK] + "Þ";
            }

            var result = accessLogs
                .Where(x => string.Compare(x.IPv4, addressStart) >= 0)
                .Where(x => string.Compare(x.IPv4, addressEnd) <= 0)
                .Where(x => x.AccessTime >= timeStart)
                .Where(x => x.AccessTime <= timeEnd)
                .GroupBy(x => x.IPv4)
                .Select(g =>  $"{g.Key} {g.Count()}")
                .ToList();

            try
            {
                File.WriteAllLines(_argumentValues[FILE_OUTPUT], result);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Ошибка при записи в файл. {ex.Message}");
            }
        }

        private static (DateTime, DateTime) GetTimeInterval() 
        {
            DateTime timeStart = DateTime.MinValue, timeEnd = DateTime.MaxValue;
            if (!string.IsNullOrEmpty(_argumentValues[TIME_START]))
            {                
                timeStart = DateTime.ParseExact(_argumentValues[TIME_START], DATETIME_ARGS_FORMAT, CultureInfo.InvariantCulture);             
                if (!string.IsNullOrEmpty(_argumentValues[TIME_END])) 
                {
                    timeEnd = DateTime.ParseExact(_argumentValues[TIME_END], DATETIME_ARGS_FORMAT, CultureInfo.InvariantCulture);
                }
            }

            return (timeStart, timeEnd);
        }

        private static List<AccessLog> GetAccessLogs(string fileLog) 
        {                        
            return File.ReadLines(fileLog)
                .Select(x => {         
                    return new AccessLog
                    {
                        IPv4 = x.Split(':')[0],
                        AccessTime = DateTime.ParseExact(string.Join(":", x.Split(':')[1..].ToList()), DATETIME_LOG_FORMAT, CultureInfo.InvariantCulture)
                    };
                })
                .ToList();
        }
        
        private static void GetArgumentsValues(string[] args)
        {
            foreach (var key in _argumentValues.Keys)
            {
                _argumentValues[key] = GetArgValue(args, key);
            }
        }

        private static string GetArgValue(string[] args, string argName)
        {
            return args.FirstOrDefault(x => x.Contains(argName))?.Split('=')[1]??"";
        }
    }
}