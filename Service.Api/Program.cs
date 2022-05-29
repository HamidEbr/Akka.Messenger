using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Akka.Messenger.Shared.Models;
using Akka.Messenger.Shared.Sharding;
using Akka.Remote.Hosting;
using Service.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//var config = Akka.Messenger.Shared.Helper.ConfigHelper.GetAkkaConfig();

builder.Services.AddAkka("messenger-system", configurationBuilder =>
{
    configurationBuilder
        .WithRemoting("localhost", 0)
        .WithClustering(new ClusterOptions()
        {
            Roles = new[] { "smsRole" },
            SeedNodes = new[] { Address.Parse("akka.tcp://messenger-system@localhost:7919") }
        })
        .WithShardRegion<UserEntity>("userActions", s => UserEntity.Props(s),
            new MessageExtractor(),
            new ShardOptions() { StateStoreMode = StateStoreMode.DData, Role = "smsRole" })
        .WithActors((system, registry) =>
        {
            var userActionsShard = registry.Get<UserEntity>();
            var indexer = system.ActorOf(Props.Create(() => new UserProxy(userActionsShard)), "index");
            registry.TryRegister<Index>(indexer); 
        });
});

var app = builder.Build();

#region Minimal Apis

app.MapPost("/message/send", async (ActorRegistry registry, SmsDto message) =>
{
    var index = registry.Get<Index>();
    var result = await index
        .Ask<Guid>(new ShardEnvelope(message.Sender, UserEntity.SendSmsMessage.Create(message.Receiver, new Sms(message.Message))));
    return Results.Ok(result);
});

app.MapPut("/message/edit/{id}", async (ActorRegistry registry, Guid id, SmsDto message) =>
{
    var index = registry.Get<Index>();
    var result = await index.Ask<object>(new ShardEnvelope(message.Sender,
                UserEntity.EditSmsMessage.Create(id, message.Receiver, message.Message)));
    if(result is UserEntity.BaseErrorResponse error)
    {
        return Results.BadRequest(error.Message);
    }
    return Results.Ok(result);
});

app.MapGet("/read_new_messages/phone-number/{phoneNumber}", async (ActorRegistry registry, string phoneNumber) =>
{
    var index = registry.Get<Index>();
    var result = await index.Ask<IEnumerable<SmsResponse>>(new ShardEnvelope(phoneNumber,
        UserEntity.ReadNewSmsesMessage.Instance()));
    return Results.Ok(result);
});

app.MapGet("/read_all_messages/phone-number/{phoneNumber}", async (ActorRegistry registry, string phoneNumber) =>
{
    var index = registry.Get<Index>();
    var result = await index.Ask<IEnumerable<SmsResponse>>(
        new ShardEnvelope(phoneNumber, UserEntity.ReadAllSmsesMessage.Instance()));
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
