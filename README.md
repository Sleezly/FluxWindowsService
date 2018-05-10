# FluxWindowsService
A Flux like circadian service specifically coupled to the Philips Hue platform (Q42) and runs as a Windows service.

# Features
- Runs as a background windows service to calculate color temperature for Philips Hue color and color ambiance light bulbs.
- Build with Visual Studio 2017 Community Edition (free!)
- Scheduled to run as set by configs and is fully customizeable by data.
- Can add timers to initiate a color / brightness change action for a set of lights at a set time.
- Service does not poll; sleeps until the next color temperature value needs to be sent.
- Scenes on the Hub bridge can be updated with color temperature value to ensure lights currently off will be turn on at expected color.
- Hosts a RESETful HTTP endpoint to allow for toggling service on/off and to retrieve current state of service.
- An HTTP self-host server will be started at http://localhost:51234/ with simple POST and GET commands supported.
- Support for multiple Hue hubs.
- A light-weight win32 app can be launched to retrieve Flux status via HTTP GET commands.

# Nuget packages utilized
- Q42 to facilitate communication to the Hub hub.
- Topshelf to facilitate running as a Windows service.
- SolarCalendar to determine solar noon proxima.
- log4net for logging.

# How to set config files

1. Create a client ID for your Hue Hub(s) by following the official documentation here:
https://www.developers.meethue.com/documentation/configuration-api#71_create_user

2. Once you can connect to your hub, get the Bridge ID.
- from a browser, go to http://x.x.x.x/debug/clip.html
- set the URL to /api/[user token from #1]/config
- look for "bridgeid"

3. Tell Flux what to do in HueController\FluxConfig.json:

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

4. OPTIONAL! Tell Flux which lights to control in HueController\LightEntityRegistry.yaml -- this way you can exclude certain lights:
- if you have HomeAssistant connected to your Hue Hubs, simply grab your LightEntityRegistry.yaml file and use that. No extra work required.
- if some lights should not be controlled, remove them from this file
- if all lights should be controlled, remove this file
- if you don't have HomeAssistant, to manually specify a white-list of lights format is as follows:
  light.inside_garage_1:
  light.inside_garage_2: 

5. OPTIONAL! Tell Flux if you have any custom timers you'd like to run in your HueController\FluxTimers.json file:

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
                "Time": "20:00:00",          // Time for the rule to run
                "Brightness": 165,
                "State": "On",               // This is the desired transition state for the lights
                "OnlyReactWithState": "On"   // This means the rule will only happen if the lights are in the 'On' state
            }
        ]
    }


# Starting the Flux Service

1. Allow the 51234 port to host an http endpoint

  To enable HTTP self-hosting, run this command:
  $> netsh http add urlacl url=http://+:51234/ user=Everyone

  To view all urlacls currently active, run:
  $> netsh http show urlacl
  
  If the above is not executed, starting the web app will fail.
  
  Once the app is started, check to see if the service is reachable by going to:
  http://localhost:51234/api/flux
  
2. Open the 51234 port for broadcast in windows firewall to allow 'private' incoming traffic, assuming your
   network profile is set to private. If this doesn't work, double-check profile is set to private rather than public.
   
   This allows communication between the current PC and external, local networked devices.

3. Run FluxService via Visual Studio or build and then install the Flux Service for execution as a Windows Service

  Flux\FluxService\bin\Debug>FluxService.exe install
  
  To unreigster the service, simply run 'uninstall'.
  
  
# Enable Hue Dimmer Swtich Scene Adjustments
 
- To enable Scenes to be updated, we opt-in by including the word "flux" and "switch" somewhere in the scene.

  Example: "Switch Flux Lamp Master Bedroom" attached to a Master Bedroom Hue Dimmer will turn the lights on at Flux temperature.
  
  Note: iOS app iConnectHue attaches the word "switch" to Hue Scenes set on the Hue dimmer by default.
  
  To check what the exact name of your Hue scene is, you can use the Clip debug tool:
  http://x.x.x.x/debug/clip.html -> /api/[id]/scenes -> GET
  
  
# Troubleshooting

- When in doubt, check the logs!

  Flux\FluxService\bin\Debug\myapp.log


