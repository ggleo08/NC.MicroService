using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection"));
            });

            // 2. ע���Ŷ�service
            services.AddScoped<ITeamService, Services.TeamService>();

            // 3. ע���ŶӲִ�
            services.AddScoped<ITeamRepository, TeamRepository>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // ע��Consul
            // 1. ����consul�ͻ�������
            var consulClient = new ConsulClient(config =>
            {
                // �����ͻ��˺ͷ��������
                config.Address = new Uri("https://127.0.0.1:8500");
            });

            // 2. ����consul����ע�����
            var registration = new AgentServiceRegistration()
            {
                ID = Guid.NewGuid().ToString(),
                Name = "teamservice",
                Address = "https://localhost",
                Port = 5004,
                Check = new AgentServiceCheck
                {
                    // 3.1��consul������鳬ʱ��
                    Timeout = TimeSpan.FromSeconds(10),
                    // 3.2������ֹͣ5���ע������
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                    // 3.3��consul��������ַ
                    HTTP = "https://localhost:5004/HealthCheck",
                    // 3.4 consul���������ʱ��
                    Interval = TimeSpan.FromSeconds(3),
                }
            };

            // 3. ע�����
            consulClient.Agent.ServiceRegister(registration).Wait();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
