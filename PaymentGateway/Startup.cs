using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentGateway.Authorization;
using Microsoft.EntityFrameworkCore;
using PaymentGateway.Authorization.Data;
using PaymentGateway.Authorization.Services;
using PaymentGateway.Payments.Services;
using PaymentGateway.Mapper;
using RabbitMq.Infrastructure;

namespace PaymentGateway
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
            var config = Configuration.GetSection("jwt");
            var rabbitMqConfig = Configuration.GetSection("rabbitMq");
            services.AddTransient<JwtHandler>();
            services.AddTransient<UserAccountRepository>();
            services.AddTransient<PaymentService>();
            services.AddTransient<PasswordService>();
            services.AddTransient<AuthService>();
            services.AddTransient<IRabbitMqPublisher, RabbitMqService>();
            services.Configure<JwtSettings>(config);
            services.Configure<RabbitMqConfig>(rabbitMqConfig);
            services.AddControllers();

            services.AddDbContext<UserAccountDbContext>(options =>
            {
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"));
            });

            var jwtSettings = new JwtSettings();
            config.Bind(jwtSettings);
            var jwtHandler = new JwtHandler(Options.Create(jwtSettings));

            services.AddAuthentication(configuration =>
                {
                    configuration.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    configuration.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(configuration =>
                {
                    configuration.SaveToken = true;
                    configuration.TokenValidationParameters = jwtHandler.Parameters;
                });

            var mapperConfig = MapperConfigurationFactory.MapperConfiguration;

            services.AddSingleton(mapperConfig.CreateMapper());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
