using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NC.MicroService.IdentityClient
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
            // 1. ��������֤
            // ����ʹ��cookie�����ص�¼�û���ͨ��Cookies��ΪDefaultScheme�������ҽ� DefaultChallengeScheme����Ϊoidc��
            // ��Ϊ��������Ҫ�û���¼ʱ�����ǽ�ʹ�� OpenId Connect Э��
            services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = "Cookies";
                        options.DefaultChallengeScheme = "oidc"; // openid connect
                    })
                    .AddCookie("Cookies") // ��ӿ��Դ���Cookie �Ĵ������
                    .AddOpenIdConnect("oidc", options =>
                    {
                        // 1. ����id_token
                        options.Authority = "https://10.17.9.6:5005";    // ���������Ʒ����ַ����Ȩ��ַ
                        options.ClientId = "client-code";
                        options.ClientSecret = "secret";
                        options.ResponseType = "code";
                        options.SaveTokens = true;  // ���ڽ�����IdentityServer�����Ʊ�����cookie��

                        // 2. �����Ȩ����api��֧��(access_token)
                        options.Scope.Add("TeamService");
                        options.Scope.Add("offline_access");

                        options.RequireHttpsMetadata = true;
                        options.BackchannelHttpHandler = GetHandler();
                    });


            // ע�� IHttpClieFactory
            services.AddHttpClient("disableHttpsValidation")
                    .ConfigurePrimaryHttpMessageHandler(() =>
                    {
                        // ���������£����� HTTPS ֤����֤
                        return new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = (httpRequestMessage, x509Cert, x509Chain, errors) =>
                            {
                                return true;
                            },
                        };
                    });
            services.AddControllersWithViews();
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
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            return handler;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // ʹ�þ�̬�ļ�
            app.UseStaticFiles();

            app.UseRouting();

            // 1. ��������֤
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}").RequireAuthorization(); // openid ģʽ����ת��Ȩ
            });
        }
    }
}
