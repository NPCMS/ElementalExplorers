#!/usr/bin/python
import sys
import os
import re
import io

if len(sys.argv) > 1:
	uritext=sys.argv[1]
	domaintext=sys.argv[2]
	tokenIssuertext=sys.argv[3]
	tokenKeytext=sys.argv[4]
	if len(sys.argv) > 5:
		fileToOpen=sys.argv[5]
	else:
		absFilePath = os.path.abspath(__file__)
		fileDir = os.path.dirname(os.path.abspath(__file__))
		fileToOpen = os.path.join(fileDir,'Assets','Vivox','ChatChannelSample','Scenes', 'MainScene.unity')
		print(fileToOpen)

	with io.open(str(fileToOpen), 'r', encoding='utf8') as f:
		lines = f.readlines()

	uristring = "_server: "
	domainstring = "_domain: "
	tokenIssuer = "_tokenIssuer: "
	tokenKey = "_tokenKey: "

	with io.open(str(fileToOpen), 'w', encoding='utf8', newline='') as f:
		for line in lines:
			if uristring in line:
				line = line.replace(line, "  " + uristring + uritext + '\n')
				f.write(line)
			elif domainstring in line:
				line = line.replace(line, "  " + domainstring + domaintext + '\n')
				f.write(line)
			elif tokenIssuer in line:
				line = line.replace(line, "  " + tokenIssuer + tokenIssuertext + '\n')
				f.write(line)
			elif tokenKey in line:
				line = line.replace(line, "  " + tokenKey + tokenKeytext + '\n')
				f.write(line)
			else:
				f.write(line)
else:
	print('\nThis is the usage function\n')
	print('Usage: 1st param is Developer Portal uri')
	print('Usage: 2nd param is Developer Portal domain')
	print('Usage: 3rd param is Developer Portal token issuer')
	print('Usage: 4th param is Developer Portal token key')
	print('Usage: (optional) 5th param is a path to the cs file to modify')
	sys.exit("You must pass at least 4 params")
	sys.exit(2)