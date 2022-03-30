using System;
using System.Collections.Generic;

namespace Microsoft.TryDotNet.E2E.Tests;

public class AspNetProcess 
{
    private const string ListeningMessagePrefix = "Now listening on: ";
    
 
    public static  Uri? ResolveListeningUrl(string[] outputStrings)
    {
        // Wait until the app is accepting HTTP requests
        var listeningMessage = GetListeningMessage(outputStrings);

        if (!string.IsNullOrEmpty(listeningMessage))
        {
            listeningMessage = listeningMessage.Trim();
            // Verify we have a valid URL to make requests to
            var listeningUrlString = listeningMessage[(listeningMessage.IndexOf(
                ListeningMessagePrefix, StringComparison.Ordinal) + ListeningMessagePrefix.Length)..];
            
            listeningUrlString = string.Concat(listeningUrlString.AsSpan(0, listeningUrlString.IndexOf(':')),
                "://localhost",
                listeningUrlString.AsSpan(listeningUrlString.LastIndexOf(':')));
            
            return new Uri(listeningUrlString, UriKind.Absolute);
        }

        return null;
    }

    private static string GetListeningMessage(string[] lines)
    {
        var buffer = new List<string>();
        try
        {
            foreach (var line in lines)
            {
                buffer.Add(line);
                if (line.Trim().Contains(ListeningMessagePrefix, StringComparison.Ordinal))
                {
                    return line;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        throw new InvalidOperationException(@$"Couldn't find listening url:
{string.Join(Environment.NewLine, buffer)}");
    }
    
}