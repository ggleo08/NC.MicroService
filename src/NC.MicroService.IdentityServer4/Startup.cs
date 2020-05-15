using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NC.MicroService.IdentityServer4.DbContext;
using NC.MicroService.IdentityServer4.Models;

namespace NC.MicroService.IdentityServer4
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
            #region �ڴ�洢�����ڼ򵥹����Բ���
            //// 1. IOC��������� IdentityServer4
            //services.AddIdentityServer()
            //        .AddDeveloperSigningCredential() // �û���¼���ã�����������Ҫ���þ����¼ǩ��ƾ��
            //         // �������ݣ��ڴ�洢
            //        .AddInMemoryApiResources(IdentityServer4Config.GetApiResources()) // �洢API��Դ���˴�ʹ���˲������ݣ�
            //        .AddInMemoryClients(IdentityServer4Config.GetClients())// �洢�ͻ��ˣ�ģʽ���������������IdentityServer4�Ŀͻ���
            //        .AddTestUsers(IdentityServer4Config.GetUsers()) // �ͻ����û�����������
            //        .AddInMemoryIdentityResources(IdentityServer4Config.Ids); // openid �����Դ����������
            #endregion

            #region ��Config���ó־û�
            // 1. 
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var connectionString = Configuration.GetConnectionString("DefaultConnection");// AiConnection DefaultConnection
            services.AddIdentityServer()
                    //��api/client/identityǰ׺�� 
                    .AddConfigurationStore(options =>
                    {
                        options.ConfigureDbContext = builder =>
                        {
                            builder.UseMySql(connectionString, options =>
                            {
                                options.MigrationsAssembly(migrationsAssembly);
                            });
                        };
                    })
                    //�� devicecodes persistedgrants
                    .AddOperationalStore(options => 
                    {
                        options.ConfigureDbContext = builder =>
                        {
                            builder.UseMySql(connectionString, options =>
                            {
                                options.MigrationsAssembly(migrationsAssembly);
                            });
                        };
                    })
                    //.AddTestUsers(IdentityServer4Config.GetUsers())  // ͬ�����û�����Ҳ��Ҫ��Ϊ�־û�
                    .AddDeveloperSigningCredential();
            #endregion

            #region �û����ݳ־û�
            // 2. ע�����ݿ������ġ�
            services.AddDbContext<IdentityServerDbContext>(options =>
            {
                options.UseMySql(Configuration.GetConnectionString("DefaultConnection"));// AiConnection DefaultConnection
            });

            // �û���ɫ���ã���aspnetǰ׺
            services.AddIdentity<User, Role>(options =>
            {
                // 3.1 ���븴�Ӷ�����
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<IdentityServerDbContext>()
            .AddDefaultTokenProviders(); // Ĭ��token�ṩ����Ҳ���Բ���
            #endregion

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // ��ʼ��������������
            //InitializeDatabase(app);
            // ��ʼ���û�����
            InitializeUserDatabase(app);

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
            app.UseStaticFiles();

            // 1. ʹ�� IdentityServer4
            app.UseIdentityServer();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

        }

        #region ��ʼ���������ݣ��ͻ��ˡ�API��Դ�������û�

        // 1. ��config�����ݴ洢����
        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in IdentityServer4Config.GetClients())
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    /*foreach (var resource in Config.Ids)
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }*/
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in IdentityServer4Config.GetApiResources())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }

        // 2. ���û������ݴ洢����
        private void InitializeUserDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<IdentityServerDbContext>();
                context.Database.Migrate();

                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var idnetityUser = userManager.FindByNameAsync("leo").Result;
                if (idnetityUser == null)
                {
                    idnetityUser = new User
                    {
                        UserName = "leo",
                        Email = "leo@email.com"
                    };
                    var result = userManager.CreateAsync(idnetityUser, "123456").Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }
                    result = userManager.AddClaimsAsync(idnetityUser, new Claim[] {
                        new Claim(JwtClaimTypes.Name, "leo"),
                        new Claim(JwtClaimTypes.GivenName, "leo"),
                        new Claim(JwtClaimTypes.FamilyName, "leo"),
                        new Claim(JwtClaimTypes.Email, "leo@email.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.WebSite, "http://leo.com")
                    }).Result;

                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }
                }
            }
        }

        #endregion

    }
}
