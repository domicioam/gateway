using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

using PaymentGateway.Authorization;
using PaymentGateway.Authorization.Data;
using PaymentGateway.Authorization.Services;
using PaymentGateway.Payments.Services;
using PaymentGateway.Mapper;
using MongoDbRepository;
using RabbitMQService;
using PaymentGateway.Hubs;
using Microsoft.OpenApi.Models;

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

            services.Configure<MongoDbSettings>(Configuration.GetSection(nameof(MongoDbSettings)));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);
            services.AddTransient<JwtHandler>();
            services.AddTransient<UserAccountRepository>();
            services.AddTransient<PaymentService>();
            services.AddTransient<PasswordService>();
            services.AddTransient<AuthService>();
            services.AddTransient<RabbitMqPublisher>();
            services.Configure<JwtSettings>(config);
            services.Configure<RabbitMqConfig>(rabbitMqConfig);
            services.AddTransient<PaymentReadRepository>();
            services.AddSingleton<IHostedService, PaymentBackgroundService>();
            services.AddTransient<RabbitMqConsumer>();
            
            services.AddSignalR();
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payment Gateway API", Version = "v1", Description = "API used to request payments from your buyers." });
            });

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
                endpoints.MapHub<PaymentResponseHub>("/responseHub");
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Gateway API V1");
            });
        }
    }
}
