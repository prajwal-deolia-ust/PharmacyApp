using PharmacyApp.Repositories;
using PharmacyApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Repositories — data access layer
builder.Services.AddSingleton<IMedicineRepository, JsonMedicineRepository>();
builder.Services.AddSingleton<ISaleRepository, JsonSaleRepository>();

// Services — business logic layer
builder.Services.AddSingleton<IMedicineService, MedicineService>();
builder.Services.AddSingleton<ISaleService, SaleService>();

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
