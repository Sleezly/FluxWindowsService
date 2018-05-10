# FluxWindowsService
A Flux like circadian service specifically coupled to the Philips Hue platform (Q42) and runs as a Windows service.

Features:
- Runs as a background windows service to calculate color temperature to set to Philips Hue color and color ambiance light bulbs.
- Schedule to run is set via config and fully customizeable.
- Can add timers to initiate a color / brightness change action for a set of lights at a set time.
- Service does not poll; sleeps until the next color temperature value needs to be sent.
- Hosts a RESETful HTTP endpoint to allow for toggling service on/off and to retrieve current state.
- Support for multiple Hue hubs.
- an HTTP selfhost server will be started at http://localhost:51234/ with simple POST and GET commands supported.
- a light-weight win32 app can be launched to retrieve Flux status via HTTP GET commands.

Nuget packages utilized:
- Q42 to facilitate communication to the Hub hub.
- Topshelf to facilitate running as a Windows service.
- SolarCalendar to determine solar noon proxima.
- log4net for logging.

How to set up:

1. Create a client ID for your Hue Hub(s) by following the official documentation here:
https://www.developers.meethue.com/documentation/configuration-api#71_create_user

2. Once you can connect to your Hue hub, get the Bridge ID for each of your Hue hubs.
- from a browser, go to http://x.x.x.x/debug/clip.html
- set the URL to /api/[user token from #1]/config
- look for "bridgeid"

3. Tell Flux what to do in your HueController\FluxConfig.json:

    {
        "LightTransitionDuration": "00:00:15",           // Default transition duration for each flux action
        "BridgeIds": {
            "[bridgeid from #2]": "[client id from #1]", // Hub #1
            "[bridgeid from #2]": "[client id from #1]", // Hub #2
        },
        "StopTime": "22:00:00",                          // Flux service will stop at this time for the evening
        "Latitude": xxx,                                 // Your geo-location, used to determine sunrise / noon / sunset
        "Longitude": yyy,                                // Your geo-location, used to determine sunrise / noon / sunset
        "SunriseColorTemperature": 3000,                 // Color temperature to start the morning
        "SolarNoonTemperature": 5000,                    // Noon color temperature
        "SunsetColorTemperature": 2400,                  // Dusk color temperature
        "StopColorTemperature": 2000                     // Ending color temperature for the evening
    }

Steps #4 and #5 are optional!

4. Tell Flux which lights to control in your HueController\LightEntityRegistry.yaml -- this way you can exclude certain lights complete:
- if you have HomeAssistant connected to your Hue Hubs, simply grab your LightEntityRegistry.yaml file and use that. No extra work required.
- if some lights should not be controller, remove them from this file
- if all lights should be controlled, skip this file
- if you don't have HomeAssistant, specify white-list of lights to include as follows:
  light.inside_garage_1
  light.inside_garage_2: 

5. Tell Flux if you have any custom timers you'd like to run in your HueController\FluxTimers.json file:

    {
        "Rules": [
            {
                "Name": "Evening Entry Lights Dimmed",
                "LightIds": [
                    "Entry 1",
                    "Entry 2",
                    "Entry 3"
                ],
                "BridgeId": "[bridgeid]",
                "TransitionDuration": "00:5:00",
                "Time": "20:00:00",
                "Brightness": 165,
                "State": "On",
                "OnlyReactWithState": "On"   // This means the rule will only happen if the lights are in the On state
            }
        ]
    }


6. Install the Flux Windows Service to self-host the HTTP endpoint.

  To enable HTTP self-hosting, run this command:
  $> netsh http add urlacl url=http://+:51234/ user=Everyone

  To view all urlacls currently active, run:
  $> netsh http show urlacl
  
  If the above is not executed, starting the web app will fail.
  
7. Enable the 51234 port for broadcast through windows firewall to allow incoming traffic in 'private', assuming your
   network profile is set to private. If this doesn't work, double-check profile is set to private rather than public.

8. Install the Flux Service for execution as a Windows Service

  Flux\FluxService\bin\Debug>FluxService.exe install
  
  To unreigster the service, simply run 'uninstall'.
  


   - Either run the service via Visual Studio with F5 for local debugging.
