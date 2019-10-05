This is a very simple ping logging application.
It is multithreaded, and can ping multiple hosts at the same time, all with different intervals.
On first startup, it will prompt you to add one or more hosts.
Each host will prompt if you want to configure advanced options

The advanced options are as follows:

Threshold: This is the time in milliseconds that, when hit, changes the log entry from [INF] to [WRN], allowing for an easier time tracking down network slowdowns
Timeout: This is the maximum time that the ping logger will wait for a response before giving up. 
Interval: Specifies the time between each ping. 
Packet Size: Specifies the size of the packet to be sent. 

If you wish to edit these settings after the inital setup, they're stored in JSON format in the 'opts.json' file that is generated. 

The application will then log to both the console output and individual log files. 

Requires dotnet core 3.0 or above. 
Uses Serilog for actual file and console logging. 
