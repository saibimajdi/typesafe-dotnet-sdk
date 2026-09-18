using TypeSafeAI.Internal;

namespace TypeSafeAI;

/// <summary>
/// The default <see cref="IModelsResource"/> implementation.
/// </summary>
internal sealed class ModelsResource : IModelsResource
{
    private readonly TypeSafeTransport _transport;
    private readonly Uri _uri;

    public ModelsResource(TypeSafeTransport transport, Uri uri)
    {
        _transport = transport;
        _uri = uri;
    }

    /// <inheritdoc />
    public async Task<ModelsResult> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _transport
            .SendAsync(HttpMethod.Get, _uri, body: null, "models.list", options: null, cancellationToken)
            .ConfigureAwait(false);

        return TypeSafeResponseReader.ReadModels(response);
    }
}
