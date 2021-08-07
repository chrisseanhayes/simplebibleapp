#!/bin/bash

systemctl stop kestrel-simplebibleapp-auth.service
dotnet publish ~/simplebibleapp/src/simplebibleapp/IdentityServerWithAspNetIdentity/ --output /var/aspnetcore/simplebibleapp/ --configuration release
systemctl start kestrel-simplebibleapp-auth.service
