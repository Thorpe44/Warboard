/// <summary>
/// v43.1 first-compile shim.
///
/// The v43 one-time Editor installer adds the real
/// ModelToken.ApplyFactionMaxWoundsModifier instance method. Unity must first
/// compile the project before that installer can run, so this extension exists
/// solely to satisfy the compiler during that first pass.
///
/// The installer deletes this file after the real ModelToken method has been
/// installed.
/// </summary>
public static class CustodesModelTokenCompileShim
{
    public static void ApplyFactionMaxWoundsModifier(
        this ModelToken model,
        int amount)
    {
        // Intentionally empty. The v43 installer runs immediately after this
        // compile and installs the real instance method before gameplay.
    }
}
