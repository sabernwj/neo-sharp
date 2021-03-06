using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NeoSharp.BinarySerialization;
using NeoSharp.Core.Blockchain.Processors;
using NeoSharp.Core.Blockchain.State;
using NeoSharp.Core.Cryptography;
using NeoSharp.Core.Models;
using NeoSharp.Core.Persistence;
using NeoSharp.Core.Types;
using NeoSharp.TestHelpers;

namespace NeoSharp.Core.Test.Blockchain.Processors
{
    [TestClass]
    public class UtStateTransactionProcessor : TestBase
    {
        [TestMethod]
        public async Task Process_AccountStateDescriptor_UpdateVotes()
        {
            var scriptHash = new UInt160(RandomByteArray(20));
            var value = new byte[1];
            var input = new StateTransaction
            {
                Descriptors = new[]
                {
                    new StateDescriptor
                    {
                        Type = StateType.Account,
                        Field = "Votes",
                        Key = scriptHash.ToArray(),
                        Value = value
                    }
                }
            };
            var ecpoints = new ECPoint[0];
            var deserializerMock = AutoMockContainer.GetMock<IBinaryDeserializer>();
            deserializerMock.Setup(m => m.Deserialize<ECPoint[]>(value, null)).Returns(ecpoints);
            var accountManagerMock = AutoMockContainer.GetMock<IAccountManager>();
            var testee = AutoMockContainer.Create<StateTransactionProcessor>();

            await testee.Process(input);

            accountManagerMock.Verify(m => m.UpdateVotes(It.Is<UInt160>(u => u.Equals(scriptHash)), ecpoints));
        }

        [TestMethod]
        public async Task Process_ValidatorStateDescriptor_NewValidator()
        {
            var pubKey = new byte[33];
            pubKey[0] = 0x02;
            var input = new StateTransaction
            {
                Descriptors = new[]
                {
                    new StateDescriptor
                    {
                        Type = StateType.Validator,
                        Key = pubKey,
                        Field = "Registered",
                        Value = new byte[] {0x01}
                    }
                }
            };
            var repositoryMock = AutoMockContainer.GetMock<IRepository>();
            repositoryMock.Setup(m => m.GetValidator(It.Is<ECPoint>(p => p.CompareTo(new ECPoint(pubKey)) == 0)))
                .ReturnsAsync((Validator) null);
            var testee = AutoMockContainer.Create<StateTransactionProcessor>();

            await testee.Process(input);

            repositoryMock.Verify(m => m.AddValidator(It.Is<Validator>(v =>
                v.PublicKey.CompareTo(new ECPoint(pubKey)) == 0 &&
                v.Registered &&
                v.Votes.Equals(Fixed8.Zero))));
        }

        [TestMethod]
        public async Task Process_ValidatorStateDescriptor_UpdateValidatorToTrue()
        {
            var pubKey = new byte[33];
            pubKey[0] = 0x02;
            var input = new StateTransaction
            {
                Descriptors = new[]
                {
                    new StateDescriptor
                    {
                        Type = StateType.Validator,
                        Key = pubKey,
                        Field = "Registered",
                        Value = new byte[] {0x01}
                    }
                }
            };
            var expectedVotes = RandomInt();
            var expectedValidator = new Validator
            {
                PublicKey = new ECPoint(pubKey),
                Registered = false,
                Votes = new Fixed8(expectedVotes)
            };
            var repositoryMock = AutoMockContainer.GetMock<IRepository>();
            repositoryMock.Setup(m => m.GetValidator(It.Is<ECPoint>(p => p.CompareTo(new ECPoint(pubKey)) == 0)))
                .ReturnsAsync(expectedValidator);
            var testee = AutoMockContainer.Create<StateTransactionProcessor>();

            await testee.Process(input);

            repositoryMock.Verify(m => m.AddValidator(It.Is<Validator>(v =>
                v == expectedValidator && v.Votes.Equals(new Fixed8(expectedVotes)) && v.Registered)));
        }
    }
}