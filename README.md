#Cinegy Teletext Decoder Library

Use this library to decode classic teletext caption data from DVB transport streams - tested with UK, Swedish and Australian off-air streams. The library takes a dependency on the Cinegy Transport Stream Decoder Library (Apache 2 licensed).;

##How easy is it?

The library was designed to be simple to use and flexible. Use the Cinegy TS decoder to create packets of data from a stream or a file, and pass these packets to the teletext decoder and get results!

You can print live Teletext decoding, and you can use the tool to generate input logs for 'big data' analysis (which is very cool).

See all of this in action inside the Cinegy TS Analyser tool here: [GitHub] [https://github.com/cinegy/tsanalyser]
    
##Getting the library

Just to make your life easier, we auto-build this using AppVeyor and push to NuGet - here is how we are doing right now: 

[![Build status](https://ci.appveyor.com/api/projects/status/n3d93ssm0abw87sd?svg=true)](https://ci.appveyor.com/project/cinegy/ttxdecoder)

You can check out the latest compiled binary from the master or pre-master code here:

[AppVeyor TtxDecoder Project Builder](https://ci.appveyor.com/project/cinegy/ttxdecoder/build/artifacts)

Available on NuGet here:

[NuGet](https://www.nuget.org/packages/Cinegy.TtxDecoder/)

##Credits

<<<<<<< HEAD
Massive credit to Christoffer Branzell from Vericom, for writing the core of the original decoder - it's since been dramatically moved about removing all trace of his work, but all the hard bits came from him!
=======
Massive credit to Christoffer Branzell from Vericom, for writing the core of the original decoder - it's since been dramatically moved about removing all trace of his work, but all the hard bits came from him!
>>>>>>> fcaeddb143db9a85f1aa0e72957aaed4634bcdbe
