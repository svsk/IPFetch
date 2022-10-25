# IPFetch

This is an application intended to notify the user about changes in the public IP address of the environment in
which it runs. Upon changes there is functionality to send an e-mail notification through a MailGun account as well
as running HTTP GETs on one or more configurable URLs. This can be used to update DNS entries from services such as
[afraid.org](https://freedns.afraid.org/).

## Installation

Download the latest release and run the install.bat file. This will prompt you for some basic information needed
to set up the application. Once completed the app will be running as a Windows service on the environment.
