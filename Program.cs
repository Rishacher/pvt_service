using Abstractions;
using PvtService.Services;

namespace pvt_service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
            RegisterServices(builder.Services);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();
            

            // app.UseHttpsRedirection();

            app.UseAuthorization();

            // app.MapPost("/calculator", ([FromBody] PvtParams pvtParams, IPvtCalculationService pvtCalculationService) => pvtCalculationService.CalculateMixProperties(pvtParams));
            app.MapControllers();
            
            app.UseCors((options) => { options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
            
            app.Run();
        }

        private static void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IPvtCalculationService, PvtCalculationService>();
        }
    }
}