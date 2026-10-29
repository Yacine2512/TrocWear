using TrocWear.BuildingBlocks.EventContracts;
namespace AuthService.Application.Interface.IService;
public interface IOutboxWriter { void Add(EventEnvelope evt); }
