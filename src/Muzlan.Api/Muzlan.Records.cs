using System;
using System.Collections.Generic;

namespace Muzlan.Api
{
    public record AuthRecord(string CsrfToken, string MediaToken);
    public record ArtistRecord(string Name, Uri ArtistUri, Uri SearchUri, Uri ImageUri);
    public record TagRecord(string Name, Uri TagUri);
    public record SearchQueryRecord(string Name, Uri SearchUri);
    public record SearchArtistRecord(ArtistRecord Artist, IList<TagRecord> Tags, string Description);
    public record TrackRecord(string Name, string Artist, Uri TrackUri, Uri DownloadUri);
    public record AlbumRecord(string Artist, string Title, Uri AlbumUri, Uri ImageUri);
    public record SitemapRecord(string Name, Uri ItemUri);
    public record DownloadRecord(string Filename, byte[] Data, Uri SourceUri);
}
