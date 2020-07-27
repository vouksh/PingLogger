This is a ping logging application that can be helpful in finding network drops.
It uses the same basic ICMP protocol as the normal Ping program uses. 

V2 is the graphical user interface version, and is simpler to use, but comes with a slightly larger footprint.

How to use:

On first open, you will need to set up at least one remote host to ping. 

The options for it  are as follows: 

Interval: The time the program will wait between sending pings. The program does not wait for a ping to come back before sending another.

Warning Threshold: If the total round trip time of the ping meets or exceeds this number, it will be marked as a warning in both the GUI and in a separate log file.

Timeout Threshold: The maximum amount of time that the program will wait for a packet to return before considering the ping as timed out. 

Packet Size: This is the size in bytes of the ICMP packets that are sent. Maximum packet size is 65,500 bytes (roughly 64kb). Larger packets are fragmented automatically. 


Options

Start all pingers on application start: Once application is loaded, all pingers will start automatically. They can still be stopped and edited as needed.

Load Application On Windows Startup: Puts a batch file in the users startup folder that opens the application.

Number of days to keep log files: This is how many days log files are kept for each host. If a host is removed, the old log files are kept and need to be deleted manually. Defaults to 7 days.

Start All Loggers: Starts any loggers that aren't already running. 

Stop All Loggers: Stops any currently running loggers. 