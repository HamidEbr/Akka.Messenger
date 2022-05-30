using Akka.Actor;
using Akka.Messenger.Shared.Models;
using Akka.Messenger.Shared.Sharding;
using Akka.TestKit;
using Akka.TestKit.TestActors;
using Xunit.Abstractions;

namespace Akka.Messenger.Tests
{
    public class AkkaMessengerTests : TestKit.Xunit2.TestKit
    {
        private TestProbe _sendSmsMockProbe;
        private TestProbe _reciveSmsMockProbe;
        private TestProbe _editSmsMockProbe;
        private const string FakeSenderPhone = "09123456789";
        private const string FakeReceiverPhone = "09121234567";
        private const string FakeText1 = "Hello";
        private const string FakeText2 = "Hi";

        public AkkaMessengerTests(ITestOutputHelper output) : base(output: output)
        {
            Initialize();
        }

        private void Initialize()
        {
            SendMockInit();
            ReciveMockInit();
            EditMockInit();
        }


        #region Send/Recive sms

        private void ReciveMockInit()
        {
            _reciveSmsMockProbe = CreateTestProbe();
            var reciveSmsAutoPilot = new DelegateAutoPilot((sender, message) =>
            {
                if (message is ShardEnvelope envelope)
                {
                    switch (envelope.Payload)
                    {
                        case UserEntity.DeliverMessage:
                        case UserEntity.AckReadNewSmsMessage:
                            break;
                        default: Assert.True(false); break;
                    }
                }
                return AutoPilot.KeepRunning;
            });
            _reciveSmsMockProbe.SetAutoPilot(reciveSmsAutoPilot);
        }

        private void SendMockInit()
        {
            _sendSmsMockProbe = CreateTestProbe();
            var sendSmsAutoPilot = new DelegateAutoPilot((sender, message) =>
            {
                Assert.True(message is ShardEnvelope envelope && envelope.Payload is UserEntity.ReceiveSmsMessage);
                return AutoPilot.KeepRunning;
            });
            _sendSmsMockProbe.SetAutoPilot(sendSmsAutoPilot);
        }

        private async Task<Guid> SendFakeSms(IActorRef fakeSender, string fakereceiver, string fakeMessage)
        {
            var sendResult = await fakeSender.Ask(UserEntity.SendSmsMessage.Create(fakereceiver, new Sms(fakeMessage)));
            Assert.NotNull(sendResult);
            Assert.IsType<Guid>(sendResult);
            Assert.NotEqual(sendResult, Guid.Empty);
            return (Guid)sendResult;
        }

        [Fact]
        public async void Should_receiver_receive_sms()
        {
            var senderUser = ActorOf(UserEntity.Props(FakeSenderPhone, _sendSmsMockProbe.Ref));
            await SendFakeSms(senderUser, FakeReceiverPhone, FakeText1);
            ExpectNoMsg();
        }

        [Fact]
        public void Should_receiver_send_deliver_sms()
        {
            var fakeReceiver = ActorOf(UserEntity.Props(FakeReceiverPhone, _reciveSmsMockProbe.Ref));
            fakeReceiver.Tell(UserEntity.ReceiveSmsMessage.Create(FakeSenderPhone, new Sms(FakeText1)));
            ExpectMsg<UserEntity.ReceiveSmsMessageSuccess>();
        }

        [Fact]
        public async void Should_sender_recive_deliver_messege_success_when_there_are_any_smses()
        {
            var fakeSender = ActorOf(UserEntity.Props(FakeSenderPhone, _sendSmsMockProbe.Ref));
            var id = await SendFakeSms(fakeSender, FakeReceiverPhone, FakeText1);
            ExpectNoMsg();
            
            fakeSender.Tell(UserEntity.DeliverMessage.Create(id));
            ExpectMsg<UserEntity.DeliverMessageSuccess>();
        }

        [Fact]
        public void Should_sender_recive_sms_not_found_when_there_is_no_sms()
        {
            var fakeSender = ActorOf(UserEntity.Props(FakeSenderPhone, _sendSmsMockProbe.Ref));
            fakeSender.Tell(UserEntity.DeliverMessage.Create(Guid.NewGuid()));
            ExpectMsg<UserEntity.SmsNotFound>();
        }

        #endregion

        #region Edit sms

        private void EditMockInit()
        {
            _editSmsMockProbe = CreateTestProbe();
            var sendSmsAutoPilot = new DelegateAutoPilot((sender, message) =>
            {
                if (message is ShardEnvelope envelope)
                {
                    switch (envelope.Payload)
                    {
                        case UserEntity.ReceiveSmsMessage:
                            break;
                        case UserEntity.ReciveEditSmsMessage msg:
                            sender.Tell(new SmsResponse()
                            {
                                Id = msg.SmsId,
                                CreatedDate = DateTime.Now,
                                DeliveredDate = DateTime.Now,
                                ModifiedDate = DateTime.Now,
                                ReadDate = DateTime.Now,
                                ReceiverPhone = FakeReceiverPhone,
                                SenderPhone = FakeSenderPhone,
                                Status = Sms.SmsStatus.Delivered,
                                Text = msg.Text,
                            });
                            break;
                        default:
                            Assert.False(true);
                            break;
                    }
                }
                return AutoPilot.KeepRunning;
            });
            _editSmsMockProbe.SetAutoPilot(sendSmsAutoPilot);
        }

        [Fact]
        public void Should_sender_edit_sms_recive_sms_not_found_when_there_is_no_sms()
        {
            var fakeSender = ActorOf(UserEntity.Props(FakeSenderPhone));
            fakeSender.Tell(UserEntity.EditSmsMessage.Create(Guid.NewGuid(), FakeReceiverPhone, FakeText2));
            ExpectMsg<UserEntity.SmsNotFound>();
        }

        [Fact]
        public async void Should_sender_edit_sms_success_when_there_are_any_sms()
        {
            var fakeSender = ActorOf(UserEntity.Props(FakeSenderPhone, _editSmsMockProbe.Ref));
            var id = await SendFakeSms(fakeSender, FakeReceiverPhone, FakeText1);
            ExpectNoMsg();
            fakeSender.Tell(UserEntity.EditSmsMessage.Create(id, FakeReceiverPhone, FakeText2));
            ExpectMsg<SmsResponse>(TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void Should_receiver_edit_sms_recive_sms_not_found_when_there_is_no_sms()
        {
            var fakeReceiver = ActorOf(UserEntity.Props(FakeReceiverPhone));
            fakeReceiver.Tell(UserEntity.ReciveEditSmsMessage.Create(Guid.NewGuid(), FakeText2));
            ExpectMsg<UserEntity.SmsNotFound>();
        }

        [Fact]
        public async void Should_receiver_edit_sms_success_when_there_is_a_real_sms()
        {
            var fakeReceiver = ActorOf(UserEntity.Props(FakeReceiverPhone));
            var msg = await fakeReceiver.Ask<UserEntity.ReceiveSmsMessageSuccess>(
                UserEntity.ReceiveSmsMessage.Create(FakeSenderPhone, new Sms(FakeText1)));
            
            fakeReceiver.Tell(UserEntity.ReciveEditSmsMessage.Create(msg.Id, FakeText2));
            ExpectMsg<SmsResponse>(TimeSpan.FromSeconds(30));
        }

        #endregion

        #region Read sms

        [Fact]
        public async void Should_sender_read_new_sms_recive_empty_when_there_is_no_sms()
        {
            var fakeSender = ActorOf<BlackHoleActor>();
            var fakeUser = ActorOf(UserEntity.Props(FakeReceiverPhone, fakeSender));
            var respones = await fakeUser.Ask<IEnumerable<SmsResponse>>(UserEntity.ReadNewSmsesMessage.Instance());
            Assert.NotNull(respones);
            Assert.Empty(respones);
        }

        [Fact]
        public async void Should_sender_read_new_sms_recive_list_when_there_are_any_smses()
        {
            var fakeReceiver = ActorOf(UserEntity.Props(FakeReceiverPhone, _reciveSmsMockProbe.Ref));
            fakeReceiver.Tell(UserEntity.ReceiveSmsMessage.Create(FakeSenderPhone, new Sms(FakeText1)));
            ExpectMsg<UserEntity.ReceiveSmsMessageSuccess>();
            var respones = await fakeReceiver.Ask<IEnumerable<SmsResponse>>(UserEntity.ReadNewSmsesMessage.Instance());
            Assert.NotNull(respones);
            Assert.NotEmpty(respones);
        }

        [Fact]
        public void Should_sender_ackread_new_sms_recive_empty_when_there_is_no_sms()
        {
            var fakeSender = ActorOf<BlackHoleActor>();
            var fakeUser = ActorOf(UserEntity.Props(FakeReceiverPhone, fakeSender));
            fakeUser.Tell(UserEntity.AckReadNewSmsMessage.Create(Guid.NewGuid()));
            ExpectMsg<UserEntity.SmsNotFound>();
        }

        [Fact]
        public async void Should_sender_ackread_new_sms_recive_success_when_there_are_any_smses()
        {
            var senderUser = ActorOf(UserEntity.Props(FakeSenderPhone, _sendSmsMockProbe.Ref));
            var id = await SendFakeSms(senderUser, FakeReceiverPhone, FakeText1);
            ExpectNoMsg();
            senderUser.Tell(UserEntity.AckReadNewSmsMessage.Create(id));
            ExpectMsg<UserEntity.AckReadNewSmsMessageSuccess>();
        }

        [Fact]
        public async void Should_sender_read_all_sms_recive_empty_when_there_is_no_sms()
        {
            var fakeSender = ActorOf<BlackHoleActor>();
            var fakeUser = ActorOf(UserEntity.Props(FakeReceiverPhone, fakeSender));
            var respones = await fakeUser.Ask<IEnumerable<SmsResponse>>(UserEntity.ReadAllSmsesMessage.Instance());
            Assert.NotNull(respones);
            Assert.Empty(respones);
        }

        [Fact]
        public async void Should_sender_read_all_sms_recive_list_when_there_are_any_smses()
        {
            var fakeReceiver = ActorOf(UserEntity.Props(FakeReceiverPhone, _reciveSmsMockProbe.Ref));
            fakeReceiver.Tell(UserEntity.ReceiveSmsMessage.Create(FakeSenderPhone, new Sms(FakeText1)));
            ExpectMsg<UserEntity.ReceiveSmsMessageSuccess>();
            var respones = await fakeReceiver.Ask<IEnumerable<SmsResponse>>(UserEntity.ReadAllSmsesMessage.Instance());
            Assert.NotNull(respones);
            Assert.NotEmpty(respones);
        }

        #endregion
    }
}