This is a very simple ping logging application.
It is multithreaded, and can ping multiple hosts at the same time, all with different intervals.
On first startup, it will prompt you to add one or more hosts.
Each host will prompt if you want to configure advanced options

The advanced options are as follows:

Threshold: This is the time in milliseconds that, when hit, changes the log entry from [INF] to [WRN], allowing for an easier time tracking down network slowdowns

Timeout: This is the maximum time that the ping logger will wait for a response before giving up. 

Interval: Specifies the time between each ping. 

Packet Size: Specifies the size of the packet to be sent. 

Silent: Only logs the output to a file, not the console.

Also supports silencing all hosts and printing out a message to the console every 2 seconds. 
To edit the message, create or change the silent.txt file, then trigger a refresh within the application (Ctrl-C, option 5) or close and re-open it.

Requires dotnet core 3.0 or above to compile. 
Resulting binary will have no requirements.

Uses Serilog for actual file and console logging. 
