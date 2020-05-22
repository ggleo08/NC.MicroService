using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NC.MicroService.Gateway.IdentityServer;
using NC.MicroService.Gateway.OcelotExtension;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;

namespace NC.MicroService.Gateway
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
            #region ����IdentityServer4��У��AccessToken,�����У�����Ľ���У��
            //services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
            //  // �����п�����Ȩ��֤��ֻ��Ҫ�ڽӿ������ [Authorize] ���Լ���
            //  // ���������в�û��������Controller�Ķ������Ը��������Ȩ���ԣ���ô���������֪���Ѿ������������֤�أ�
            //  // ����������У���һ������ authenticationScheme���Ϳ��Ը��������ĸ�������Ҫ�����ȨУ�飬��TODO... 
            //  .AddIdentityServerAuthentication("OcelotKey", options =>
            //  {
            //      options.Authority = "https://192.168.2.102:5005"; // 1. ��Ȩ���ĵ�ַ
            //      options.ApiName = "OcelotService"; // 2. api����(��Ŀ��������)��ע�����ݿ��е� apiresource���Ӧ
            //      options.RequireHttpsMetadata = true; // 3. httpsԪ���ݣ�����Ҫ
            //      options.JwtBackChannelHandler = GetHandler(); // 4. �Զ��� HttpClientHandler 
            //  });
            #endregion

            #region �����ʽ���� IdentityServer4
            // �� IdentityServer ������Ϣ
            var identityServerOptions = new IdentityServerOptions();
            Configuration.Bind("IdentityServerOptions", identityServerOptions);

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
              // �����п�����Ȩ��֤��ֻ��Ҫ�ڽӿ������ [Authorize] ���Լ���
              // ���������в�û��������Controller�Ķ������Ը��������Ȩ���ԣ���ô���������֪���Ѿ������������֤�أ�
              // ����������У���һ������ authenticationScheme���Ϳ��Ը��������ĸ�������Ҫ�����ȨУ�飬��TODO... 
              .AddIdentityServerAuthentication(identityServerOptions.IdentityScheme, options =>
              {
                  options.Authority = identityServerOptions.AuthorityAddress; // 1. ��Ȩ���ĵ�ַ
                  options.ApiName = identityServerOptions.ResourceName; // 2. api����(��Ŀ��������)��ע�����ݿ��е� apiresource���Ӧ
                  options.RequireHttpsMetadata = true; // 3. httpsԪ���ݣ�����Ҫ
                  options.JwtBackChannelHandler = GetHandler(); // 4. �Զ��� HttpClientHandler 
              });
            #endregion


            // 1. ע������ Ocelot ��IOC����
            //services.AddOcelot(); // ע�� Ocelot

            //services.AddOcelot().AddConsul(); // ע�� Ocelot����� Consul��ʵ�ֶ�̬·��

            services.AddOcelot(Configuration).AddConsul().AddPolly(); // ע�� Ocelot����� Consul ʵ�ֶ�̬·�ɣ���� Polly ʵ���۶�/������
        }

        /// <summary>
        /// �Զ��� HttpClientHandler ���ܿ�֤����֤���⣺IdentityServer4 HTTPS IDX20804 IDX20803
        /// </summary>
        /// <returns></returns>
        private static HttpClientHandler GetHandler()
        {
            var handler = new HttpClientHandler();
            //handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            //handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                return true;
            };
            return handler;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // 2��ʹ������
            // app.UseOcelot().Wait();

            // 2��ʹ������
            app.UseOcelot((build, config) =>
            {
                build.BuildCustomeOcelotPipeline(config); // 2.1 �Զ���ocelot�м��ע�����
            }).Wait();


            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
