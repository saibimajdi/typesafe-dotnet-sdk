namespace TypeSafe;

/// <summary>
/// The models available to the account.
/// </summary>
/// <remarks>
/// Accessible through <see cref="ITypeSafeClient.Models"/>.
/// </remarks>
public interface IModelsResource
{
    /// <summary>
    /// Lists the models available to the account.
    /// </summary>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The available models.</returns>
    /// <exception cref="TypeSafeApiException">The API returned an unsuccessful response.</exception>
    /// <exception cref="TypeSafeConnectionException">The request never produced a response.</exception>
    Task<ModelsResult> ListAsync(CancellationToken cancellationToken = default);
}
