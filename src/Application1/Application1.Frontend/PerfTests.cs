using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Http.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Application1.Frontend
{
    public class TestResult
    {
        public string Name { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
        public double Median { get; set; }
        public double P95 { get; set; }
        public double P99 { get; set; }

        public TestResult(string name)
        {
            this.Name = name;
        }
    }

    public class PerfTests
    {
        private ServiceContext serviceContext;
        private HttpClient httpClient;
        private bool useReverseProxy;

        public PerfTests(HttpClient client, ServiceContext serviceContext, bool useReverseProxy)
        {
            this.serviceContext = serviceContext;
            this.useReverseProxy = useReverseProxy;
            if (this.useReverseProxy)
            {
                this.httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false, UseCookies = false, UseProxy = false });
            }
            else
            {
                this.httpClient = client;
            }
        }

        public TestResult Test(int testNumber, int numThread)
        {
            try
            {
                switch (testNumber)
                {
                    case 1:
                        return this.Test1("ValuesService", "web", numThread).Result;

                    default:
                        return new TestResult("Unknown test number");
                }
            }
            catch (Exception ex)
            {
                return new TestResult(ex.ToString());
            }
        }

        public async Task<TestResult> Test1(string serviceName, string endpointName, int numThread)
        {
            TestResult result = new TestResult("Test 1");
            result.Min = 999999;
            result.Max = 0;

            List<Task<TestResult>> threads = new List<Task<TestResult>>();
            for (int thread = 0; thread < numThread; thread++)
            {
                threads.Add(TestLoop1(serviceName, endpointName));
            }

            await Task.WhenAll(threads.ToArray());
            double total1 = 0, total2 = 0, total3 = 0, total4 = 0;
            foreach (var t in threads)
            {
                if (result.Min > t.Result.Min)
                    result.Min = t.Result.Min;
                if (result.Max < t.Result.Max)
                    result.Max = t.Result.Max;
                total1 += t.Result.Average;
                total2 += t.Result.Median;
                total3 += t.Result.P95;
                total4 += t.Result.P99;
            }
            result.Average = total1 / threads.Count;
            result.Median = total2 / threads.Count;
            result.P95 = total3 / threads.Count;
            result.P99 = total4 / threads.Count;
            return result;
        }

        private string CreateTestContent(int length)
        {
            Random r = new Random();
            StringBuilder sb = new StringBuilder();
            for(int i =0; i < length; i++)
            {
                sb.Append((char)(r.Next(26) + 65));
            }
            return sb.ToString();
        }

        private async Task<TestResult> TestLoop1(string serviceName, string endpointName)
        {
            int LoopNum = 2000;
            double min = 999999, max = 0, secondsTotal = 0;
            List<double> values = new List<double>();
            string testContent = CreateTestContent(20000);

            for (int i = 0; i < LoopNum; i++)
            {
                HiPerfTimer t = new HiPerfTimer();
                t.Start();
                try
                {
                    string id = Guid.NewGuid().ToString();
                    var partitionKey = this.GetValuesPartitionKey(id);
                    if (!this.useReverseProxy)
                    {
                        string requestUri = new NamedApplication(this.serviceContext)
                                                .AppendNamedService(serviceName)
                                                .AppendNamedEndpoint(endpointName: endpointName, target: ServiceTarget.Primary, partitionKey: partitionKey)
                                                .BuildHttpUri($"api/values/{id}");
                        string r = await this.httpClient.GetStringAsync(requestUri);

                        StringContent content = new StringContent(testContent);
                        HttpResponseMessage response2 = await this.httpClient.PostAsync(requestUri, content);
                        response2.EnsureSuccessStatusCode();
                    }
                    else
                    {
                        // for dev fabric, port should be 19081. For xintestcluster2, the port is 19018
                        string requestUri = $"http://localhost:19018/Application1/{serviceName}/api/values/{id}?PartitionKind=Int64Range&PartitionKey={partitionKey}";
                        string r = await this.httpClient.GetStringAsync(requestUri);

                        StringContent content = new StringContent(testContent);
                        HttpResponseMessage response2 = await this.httpClient.PostAsync(requestUri, content);
                        response2.EnsureSuccessStatusCode();
                    }
                }
                finally
                {
                    t.Stop();
                }

                if (min > t.Duration) min = t.Duration;
                if (max < t.Duration) max = t.Duration;
                secondsTotal += t.Duration;
                values.Add(t.Duration);
            }
            await Task.Delay(10000);
            var sortedList = values.OrderBy(x => x).ToList();
            return new TestResult($"Sequentially issue {LoopNum} requests.")
            {
                Min = min * 1000 / 2,
                Max = max * 1000 / 2,
                Average = secondsTotal / (double)LoopNum * 1000 / 2,
                Median = ComputeMedian(sortedList) * 1000 / 2,
                P95 = ComputePercentile(sortedList, 95) * 1000 / 2,
                P99 = ComputePercentile(sortedList, 99) * 1000 / 2
            };
        }

        private double ComputeMedian(List<double> sortedList)
        {            
            double mid = (sortedList.Count - 1) / 2.0;
            return (sortedList[(int)(mid)] + sortedList[(int)(mid + 0.5)]) / 2;
        }

        private double ComputePercentile(List<double> sortedList, int percentile)
        {
            int i = (int) ((double) sortedList.Count / 100.0 * (double) percentile);
            if (i == sortedList.Count - 1) i--;
            return (sortedList[i] + sortedList[i + 1]) / 2;
        }

        private long GetValuesPartitionKey(string id)
        {
            // When working with Service Fabric stateful service and reliable collection, one needs to understand
            // how the Service Fabric partition works, and come up with a good partition strategy for the application.
            // Please read these articles and change this method to return the partition key. 
            // https://azure.microsoft.com/en-us/documentation/articles/service-fabric-concepts-partitioning/
            // https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reliable-services-reliable-collections/
            return id == null ? 0 : ((long)id.GetHashCode()) << 32;
        }
    }
}
