using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Winton.Extensions.Configuration.Consul;

namespace NC.MicroService.VideoService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    /********** ������������ **********/
                    // 1. ��̬�����������ĵ������ļ�
                    webBuilder.ConfigureAppConfiguration((hostingContext, configBuilder) =>
                    {
                        // ����Ĭ��������Ϣ�� Configuration ����
                        hostingContext.Configuration = configBuilder.Build();

                        // ����Consul�������ĵ���������
                        string consulUrl = hostingContext.Configuration["Consul_Url"];
                        Console.WriteLine("Consul_Url��{0}", consulUrl);

                        // ��̬���ػ�����Ϣ����Ҫ���ڶ�̬��ȡ�������ƺͻ�����������
                        var env = hostingContext.HostingEnvironment;
                        configBuilder
                            // --> ���ػ��������ļ�
                            .AddConsul(
                                // $"appsettings.json", // --> ���������ã���ȡ��ʽ��Configuration["Leo-Test"]
                                // $"{env.ApplicationName}/appsettings.json", // --> ���������ʹ�÷�ʽ
                                $"{env.ApplicationName}/appsettings.{env.EnvironmentName}.json", // ����������ʹ�÷�ʽ
                                options =>
                                {
                                    // ���� consul ��ַ
                                    options.ConsulConfigurationOptions = ccOptions => { ccOptions.Address = new Uri(consulUrl); };
                                    // ���������Ƿ��ѡ
                                    options.Optional = true;
                                    // ���������ļ����º��Ƿ����¼���
                                    options.ReloadOnChange = true;
                                    // ���ú����쳣
                                    options.OnLoadException = exContext => { exContext.Ignore = false; };
                                }
                            )
                            // --> �����Զ��������ļ� 
                            .AddConsul(
                                $"{env.ApplicationName}.custom.json",
                                options =>
                                {
                                    // ���� consul ��ַ
                                    options.ConsulConfigurationOptions = ccOptions => { ccOptions.Address = new Uri(consulUrl); };
                                    // ���������Ƿ��ѡ --> �Ƿ�Ҫ���ص���˼������
                                    options.Optional = true;
                                    // ���������ļ����º��Ƿ����¼���
                                    options.ReloadOnChange = true;
                                    // ���ú����쳣
                                    options.OnLoadException = exContext => { exContext.Ignore = true; };
                                }
                            )
                            // --> ����ͨ�������ļ�
                            .AddConsul(
                                $"common.json",
                                options =>
                                {
                                    // ���� consul ��ַ
                                    options.ConsulConfigurationOptions = ccOptions => { ccOptions.Address = new Uri(consulUrl); };
                                    // ���������Ƿ��ѡ
                                    options.Optional = true;
                                    // ���������ļ����º��Ƿ����¼���
                                    options.ReloadOnChange = true;
                                    // ���ú����쳣
                                    options.OnLoadException = exContext => { exContext.Ignore = true; };
                                }
                            );
                        // Consul �м��ص�������Ϣ���ص� Configuration ����Ȼ��ͨ�� Configuration ������ص���Ŀ��
                        hostingContext.Configuration = configBuilder.Build();
                    });


                    webBuilder.UseStartup<Startup>();
                });
    }
}
