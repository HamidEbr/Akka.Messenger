using Service.Api.Actors;
using Service.Api.Helper;
using Service.Api.Models;
using Service.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAkka();

var app = builder.Build();

#region Minimal Apis

app.MapPost("/message/send", async (IMessageSessionHandler messageSessionHandler, SmsDto message) =>
{
    Guid result = await messageSessionHandler.Ask<Guid>(new ShardEnvelope(message.Sender,
                new User.SendSmsMessage(message.Receiver, new Sms(message.Message))));
    return Results.Ok(result);
});

app.MapPut("/message/edit", async (IMessageSessionHandler messageSessionHandler, Guid id, SmsDto message) =>
{
    var result = await messageSessionHandler.Ask<SmsResponse>(new ShardEnvelope(message.Sender,
                new User.EditSmsMessage(id, message.Receiver, message.Message)));
    return Results.Ok(result);
});

//app.MapGet("/message/{id}", async (IMessageSessionHandler messageSessionHandler, Guid id) =>
//{
//    var result = await messageSessionHandler.Ask<SmsResponse>(new ShardEnvelope(phoneNumber,
//        new User.ReadNewSmsesMessage()));
//    return Results.Ok(result);
//});

app.MapGet("/read_new_messages/phone-number/{phoneNumber}", async (IMessageSessionHandler messageSessionHandler, string phoneNumber) =>
{
    var result = await messageSessionHandler.Ask<IEnumerable<SmsResponse>>(new ShardEnvelope(phoneNumber,
        new User.ReadNewSmsesMessage()));
    return Results.Ok(result);
});

app.MapGet("/read_all_messages/phone-number/{phoneNumber}", async (IMessageSessionHandler messageSessionHandler, string phoneNumber) =>
{
    var result = await messageSessionHandler.Ask<IEnumerable<SmsResponse>>(
        new ShardEnvelope(phoneNumber, new User.ReadAllSmsesMessage()));
    return Results.Ok(result);
});

#endregion

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
