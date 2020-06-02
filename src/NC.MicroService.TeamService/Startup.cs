using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Consul;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NC.MicroService.EntityFrameworkCore.Repository;
using NC.MicroService.Infrastructure.Consul;
using NC.MicroService.TeamService.Domain;
using NC.MicroService.TeamService.EntityFrameworkCore;
using NC.MicroService.TeamService.Repositories;
using NC.MicroService.TeamService.Services;

namespace NC.MicroService.TeamService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // 1. ע�����ݿ�������
            services.AddDbContext<CoreContext>(options =>
            {
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection"));// AiConnection DefaultConnection
            });

            // 2. ע���Ŷ�service
            services.AddScoped<ITeamService, Services.TeamService>();

            // 3. ע���ŶӲִ�
            services.AddScoped<ITeamRepository, TeamRepository>();

            // 4. ע��ӳ��
            // services.AddAutoMapper();

            // 5. ע��Consulע�����
            services.AddConsulRegistry(Configuration);

            //// 6.У��AccessToken,�����У�����Ľ���У��-- > Ocelot ���ؼ�����Ȩ��֤
            //services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
            //        .AddIdentityServerAuthentication(options =>
            //        {
            //            options.Authority = "https://192.168.2.105:5005"; // 1. ��Ȩ���ĵ�ַ
            //            options.ApiName = "TeamService"; // 2. api����(��Ŀ��������)
            //            options.RequireHttpsMetadata = true; // 3. httpsԪ���ݣ�����Ҫ
            //            options.JwtBackChannelHandler = GetHandler(); // 4. �Զ��� HttpClientHandler 
            //        });

            //// 7. ע��Saga�ֲ�ʽ����
            //services.AddOmegaCore(options =>
            //{
            //    options.GrpcServerAddress = "LL2019:8080"; // 7.1 Э�����ĵ�ַ alpha
            //    options.InstanceId = "TeamService-ID"; // 7.2 ����ʵ��ID -- ���ڼ�Ⱥ
            //    options.ServiceName = "TeamService"; // 7.3 ��������
            //});

            services.AddControllers();
        }

        ///// <summary>
        ///// �Զ��� HttpClientHandler ���ܿ�֤����֤���⣺IdentityServer4 HTTPS IDX20804 IDX20803
        ///// </summary>
        ///// <returns></returns>
        //private static HttpClientHandler GetHandler()
        //{
        //    var handler = new HttpClientHandler();
        //    //handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        //    //handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        //    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        //    return handler;
        //}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // 1. Consul����ע��
            app.UseConsulRegistry();

            app.UseRouting();

            // 1. ���������֤ --> Ocelot ���ؼ�����Ȩ��֤
            //app.UseAuthentication();

            // 2. ʹ����Ȩ
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
