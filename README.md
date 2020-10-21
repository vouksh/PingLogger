# PingLogger #
### Version 2 is GUI only. ###
Download the latest release from [Here](https://github.com/vouksh/PingLogger/releases/latest)

## Features ##
* Offers 2 distribution methods
  * Single-file that can be placed anywhere and ran. Tradeoffs are file size and more manual clean up if you want to remove it.
  * Installer that defaults to C:\PingLogger. Smaller download, and allows for easy removal.
* Allows for many simultaneous hosts to be logged at one time.
  * Only limit is your network capabilities.
  * Verifies that the host exists before starting. 
* Extremely flexibile
  * Change your timeout, packet size, ping interval, or warning threshold. 
  * Great for power users. 
* Can start on demand, or with windows logon. 
* Interface adapts to users dark/light mode prefences in Windows.

## Help!! ##
#### Unsure of what something does? ####
* ![HostNameBox](Resources/Help/HostNameBox.jpg)
  * This is where you input the name or IP address of the remote host that you want to ping. 
  * This includes local area network hosts.
* ![IPAddressBox](Resources/Help/IPAddressBox.jpg)
  * You can't edit this field. It is populated by the program with the IP address of what is input above. 
  * In this example, 172.217.6.14 is the IP address for google.com.
* ![Interval](Resources/Help/IntervalBox.jpg)
  * This is where you specify how long (in milliseconds) to wait inbetween pings. **This is regardless of Timeout!**
  * If your timeout is longer than your interval, the responses can appear out of order!
* ![WarningThreshold](Resources/Help/WarningBox.jpg)
  * This is where you specify the response time at which you consider it to be a little too long, but not long enough to be a timeout.
  * This can help you find network slowdowns. So if something is supposed to respond in 10-15ms, but you're getting over 100ms, then it's considered a warning, and is marked as such in the logs. 
* ![localimage](Resources/Help/TimeoutBox.jpg)
  * This is where you specify how long you want to wait before considering a ping to be dropped, or timed out. 
  * *Caveat: If you set this less than ~500ms, the application will still recieve the ping back, but it will still be marked timed out!
* ![PacketSize](Resources/Help/PacketSizeBox.jpg)
  * *This should only be changed if you know what you're doing*
  * You can specify the size of the ICMP packet that gets sent. 
  * This has a hard cap of 64kb. This is a limitation of the protocol, not the application. 
  * If you increase this, your packet can and WILL be fragmented, and this will be noted in the logs as a warning. 

## Running Requirements ##
Only requires a 64-bit Windows 7 or 10 Operating System.
Application is fully self-contained and does not require .NET Core 3.1 to be installed prior. 

## Developer Requirements ##
* Visual Studio 2019 or newer (VS Code may work - not tested!)
* .NET Core 3.1 SDK

