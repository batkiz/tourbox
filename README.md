# kiwiprojekt.tourbox

Small library to read actions from [TourBox](https://www.tourboxtech.com/en/) controller by serial port.


### nuget.org
[kiwiprojekt.tourbox](https://www.nuget.org/packages/kiwiprojekt.tourbox/)

### How to use
```csharp
  class Program
  {
      static void Main()
      {
          using var handler = new TourBoxHandler("COM3", TourBoxEventHandler);
          Console.ReadLine();
      }

      private static void TourBoxEventHandler(TourBoxEvent tourBoxEvent)
      {
          if(tourBoxEvent.Is(ActionType.Click, TourBoxKey.C1, TourBoxKey.Tall))
          {
              Console.WriteLine("Hello World!");
          }
          Console.WriteLine(tourBoxEvent);
      }
  }
```

### Console app configuration
The tray app reads `appsettings.json` from its output directory.

```json
{
  "PortName": "COM3",
  "DebugMode": false
}
```

If `PortName` is omitted, the app will auto-connect only when exactly one serial port is available. Otherwise it will log an error and wait for a reload.


### License
[![MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
