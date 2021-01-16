using System;
using System.Collections.Generic;

namespace Muzlan.Api
{
    public record MuzlanArtist(string Name, Uri ArtistUri, Uri SearchUri, Uri ImageUri);
    public record MuzlanTag(string Name, Uri TagUri);
    public record MuzlanSearchQuery(string Name, Uri SearchUri);
    public record MuzlanSearchArtist(MuzlanArtist Artist, IList<MuzlanTag> Tags, string Description);
    public record MuzlanTrack(string Name, string Artist, Uri TrackUri, Uri DownloadUri);
}
