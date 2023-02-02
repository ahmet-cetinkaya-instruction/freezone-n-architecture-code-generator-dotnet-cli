using System.Runtime.CompilerServices;
using MediatR;

namespace Application.Features.Generate.Commands.Crud;

public class GenerateCrudCommandRequest : IStreamRequest<GeneratedCrudStreamCommandResponse>
{
    public class
        CreateBrandCommandRequestHandler : IStreamRequestHandler<GenerateCrudCommandRequest,
            GeneratedCrudStreamCommandResponse>
    {
        public async IAsyncEnumerable<GeneratedCrudStreamCommandResponse> Handle(GenerateCrudCommandRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new GeneratedCrudStreamCommandResponse
                { CurrentStatusMessage = "Generating first code..." };

            await Task.Delay(millisecondsDelay: 1000, cancellationToken);

            yield return new GeneratedCrudStreamCommandResponse
                { CurrentStatusMessage = "Generating second code...", LastOperationMessage = "First code." };

            await Task.Delay(millisecondsDelay: 2000, cancellationToken);

            yield return new GeneratedCrudStreamCommandResponse
                { CurrentStatusMessage = "Message sending has code.", LastOperationMessage = "Second code." };
        }
    }
}