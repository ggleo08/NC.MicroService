using System;
using System.Collections.Generic;
using System.Linq;
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
using NC.MicroService.Infrastructure.Consul;
using NC.MicroService.MemberService.EntityFrameworkCore;
using NC.MicroService.MemberService.Repositories;
using NC.MicroService.MemberService.Services;
using Servicecomb.Saga.Omega.AspNetCore.Extensions;

namespace NC.MicroService.MemberService
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
            // 1. ע�����ݿ�������
            services.AddDbContext<CoreContext>(options =>
            {
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection"));// AiConnection DefaultConnection
            });

            // 2. ע���ԱService
            services.AddScoped<IMemberService, Services.MemberService>();

            // 3. ע���Ա�ִ�
            services.AddScoped<IMemberRepository, MemberRepository>();

            // 4. ���ӳ��
            //services.AddAutoMapper();

            // 5. ��ӷ���ע������
            services.AddConsulRegistry(Configuration);

            // 6. У��AccessToken,�����У�����Ľ���У�� --> �μ� TeamService

            // 7. ע��Saga�ֲ�ʽ����
            services.AddOmegaCore(options =>
            {
                options.GrpcServerAddress = "192.168.238.237:8080"; // 7.1 Э�����ĵ�ַ alpha
                options.InstanceId = "MemberService-ID"; // 7.2 ����ʵ��ID -- ���ڼ�Ⱥ
                options.ServiceName = "MemberService"; // 7.3 ��������
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
