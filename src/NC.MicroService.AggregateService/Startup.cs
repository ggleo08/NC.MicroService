using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NC.MicroService.AggregateService.EntityFrameworkCore;
using NC.MicroService.AggregateService.Services;
using NC.MicroService.Infrastructure.Consul;
using NC.MicroService.Infrastructure.Culster;
using NC.MicroService.Infrastructure.Polly;
using Polly;
using Serilog;
using Servicecomb.Saga.Omega.AspNetCore.Extensions;

namespace NC.MicroService.AggregateService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // 1���Զ����쳣����(�û��洦��)
            var fallbackResponse = new HttpResponseMessage
            {
                Content = new StringContent("ϵͳ����æ�����Ժ�����"),// ���ݣ��Զ�������
                StatusCode = HttpStatusCode.GatewayTimeout // 504
            };

            /*********** ����PollyЧ���޸� TeamServiceHttpClient �е� GetTeams ����������ȷ�� TeamService ��Ҫ���� ***********/

            #region Polly���ã���ʱ�����ԡ��۶ϡ�����

            //services.AddHttpClient("ncmicroservices") // ʹ��������Ϊ��ʹ�÷���ֵΪ IHttpClientBuilder �Ĺ��캯�������򷵻ص��� IServiceCollection

            //        // ����
            //        .AddPolicyHandler(Policy<HttpResponseMessage>
            //                // ��ʽ1��Ĭ�������쳣����
            //                .Handle<Exception>()
            //                .FallbackAsync(fallbackResponse, async res =>
            //                {
            //                    // 1��������ӡ�쳣
            //                    Console.WriteLine($"����ʼ����,�쳣��Ϣ��{res.Exception.Message}");
            //                    // 2�������������
            //                    Console.WriteLine($"���񽵼�������Ӧ��{await fallbackResponse.Content.ReadAsStringAsync()}");
            //                    await Task.CompletedTask;
            //                }))
            //        // �۶�
            //        .AddPolicyHandler(Policy<HttpResponseMessage> // �첽���ԣ�IAsyncPolicy<HttpResponseMessage> policy
            //                .Handle<Exception>() // �����쳣
            //                                     // ������·�����۶ϻ���
            //                                     // ���� 3������ָ�������쳣�������۶�
            //                                     // ���� TimeSpan.FromSeconds(10)���۶�������ʱ��
            //                                     // ���� ��1��ί�лص���������·�������ص�֪ͨ
            //                                     // ���� ��2��ί�лص���������·�����ûص�֪ͨ
            //                                     // ���� ��2��ί�лص���������·���뿪���ص�֪ͨ��
            //                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(10), (ex, ts) =>
            //                {
            //                    Console.WriteLine($"�����·���������쳣��Ϣ��{ex.Exception.Message}");
            //                    Console.WriteLine($"�����·������ʱ�䣺{ts.TotalSeconds}s");
            //                }, () =>
            //                {
            //                    Console.WriteLine($"�����·������");
            //                }, () =>
            //                {
            //                    Console.WriteLine($"�����·���뿪��(һ�Ὺ��һ���)");
            //                }))
            //        // ����
            //        .AddPolicyHandler(Policy<HttpResponseMessage>
            //                .Handle<Exception>()
            //                .RetryAsync(3))
            //        // ��ʱ
            //        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2)));

            #endregion

            // 1.2 ��װ֮��� Polly ����
            services.AddPollyHttpClient("ncmicroservices", options =>
            {
                options.TimeoutTime = 60; // 1����ʱʱ��
                options.RetryCount = 3;// 2�����Դ���
                options.CircuitBreakerOpenFallCount = 2;// 3���۶�������(���ٴ�ʧ�ܿ���)
                options.CircuitBreakerDownTime = 100;// 4���۶�������ʱ��
                options.httpResponseMessage = fallbackResponse;// 5����������
            })
            .AddConsulClient<ConsulHttpClient>(); // 1.3��HttpClient��consul��װ(ʵ�ָ��ؾ�������)

            //// 1. ע�� Consul HttpClient����ע����չ�����а����� Consul�����֡����ؾ������ע�� --> ʡȴ2��3����
            //services.AddHttpClient().AddConsulClient<ConsulHttpClient>();

            /*// 2. ע��
            services.AddConsulDiscovery();
            // 3. ע�Ḻ�ؾ���
            services.AddSingleton<ILoadBalance, RandomLoadBalance>();*/

            // 4. ע��team����
            services.AddSingleton<ITeamServiceClient, TeamServiceHttpClient>();

            // 5. ע���Ա����
            services.AddSingleton<IMemberServiceClient, MemberServiceHttpClient>();

            // 6. ע�� Consul����ע��
            services.AddConsulRegistry(Configuration);

            // 6. У��AccessToken,�����У�����Ľ���У�� --> �μ� TeamService

            // 7. ע��Saga�ֲ�ʽ����
            services.AddOmegaCore(options =>
            {
                options.GrpcServerAddress = "192.168.91.41:8080"; // 7.1 Э�����ĵ�ַ alpha
                options.InstanceId = "AggregateService-ID"; // 7.2 ����ʵ��ID -- ���ڼ�Ⱥ
                options.ServiceName = "AggregateService"; // 7.3 ��������
            });

            // 8. ����¼����� CAP
            services.AddCap(options =>
            {
                // 8.1 ʹ���ڴ�洢��Ϣ����Ϣ����ʧ�ܴ���
                // options.UseInMemoryStorage();
                // ʹ��EntityFramework���д洢����
                options.UseEntityFramework<CoreContext>();
                // ʹ��MySql����������
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection"));

                // 8.2 ʹ��RabbitMQ�����¼����Ĵ���
                options.UseRabbitMQ(options =>
                {
                    options.HostName = "172.17.225.138";
                    options.UserName = "mq";
                    options.Password = "123456";
                    options.Port = 5672;
                    options.VirtualHost = "/";
                });
                // 8.3 ���CAP��̨���ҳ�棨�˹�����
                options.UseDashboard();
            });

            // 9. ע�����ݿ�������
            services.AddDbContext<CoreContext>(options =>
            {
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection"));// AiConnection DefaultConnection
            });

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // 1. Consul����ע��
            app.UseConsulRegistry();

            // 2. ������־Seri + ELK
            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
