using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CsvConverter
{
    public class CsvProcessor
    {
        public event EventHandler<ProgressEventArgs> ProgressUpdate;

        public List<UserInfo> FilteredList { get; private set; }

        public CsvProcessor()
        {
            FilteredList = new List<UserInfo>();
        }        

        public Task Process(string fileToProcess, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                int linesCount = await CountFileLines(fileToProcess, token);
                await ProcessFile(fileToProcess, linesCount, token);
                await FilterList(token);
                await SerializeAndSaveToFile(token);                
            });
        }

        public IEnumerable<UserInfo> Page(int pageNr, int pageSize)
        {
            return FilteredList.Skip((pageNr - 1) * pageSize).Take(pageSize);
        }

        protected virtual void OnUpdateProgress(ProgressEventArgs e)
        {
            ProgressUpdate?.Invoke(this, e);
        }

        private Task<int> CountFileLines(string fileToProcess, CancellationToken token)
        {
            return Task.Run(() =>
            {
                int linesCount = 0;

                using (StreamReader r = new StreamReader(fileToProcess))
                {
                    while (r.ReadLine() != null)
                    {
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }
                        ++linesCount;
                    }
                }

                return linesCount;
            }, token);
        }

        private Task FilterList(CancellationToken token)
        {
            return Task.Run(() =>
            {
                FilteredList = FilteredList.AsParallel()
                    .WithCancellation(token)
                    .OrderByDescending(x => x.Firstname)
                    .ThenBy(x => x.Lastname)
                    .ToList();
            }, token);
        }   

        private Task ProcessFile(string fileToProcess, int linesCount, CancellationToken token)
        {
            return Task.Run(() =>
            {
                int progress = 0;
                int linesRead = 0;
                string currentLine;
                string[] currentLineValues;

                using (StreamReader r = new StreamReader(fileToProcess))
                {
                    while ((currentLine = r.ReadLine()) != null)
                    {
                        if (token.IsCancellationRequested)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        ++linesRead;
                        if (linesRead % 100 == 0)
                        {
                            progress = linesRead * 100 / linesCount;
                            OnUpdateProgress(new ProgressEventArgs { Progress = progress });
                        }

                        currentLineValues = currentLine.Split(',');

                        // błąd jak nie ma 5 elementów - niewłaściwa struktura pliku
                        if (currentLineValues.Count() != 5)
                            throw new FileFormatException();

                        if (currentLineValues.Last().Contains(".10"))
                        {
                            FilteredList.Add(new UserInfo
                            {
                                Firstname = currentLineValues[0],
                                Lastname = currentLineValues[1],
                                Email = currentLineValues[2],
                                Country = currentLineValues[3],
                                IP_Address = currentLineValues[4]
                            });
                        }
                    }

                    OnUpdateProgress(new ProgressEventArgs { Progress = 100 });
                }
            }, token);
        }

        private Task SerializeAndSaveToFile(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                var fileContent = FilteredList.Any() ? JsonConvert.SerializeObject(FilteredList) : "brak danych";
                using (FileStream fs = File.Open("./json.txt", FileMode.Create))
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(fileContent);
                    await fs.WriteAsync(bytes, 0, Encoding.ASCII.GetByteCount(fileContent), token);
                }
            }, token);
        }
    }
}
