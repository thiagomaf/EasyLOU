--show the oldest message of the journal
ScanJournal(0)
print("First: " .. SCANJOURNALMESSAGE)

-- keep scanning the newest message of the journal
while true do
	sleep(1000)
	
	ScanJournal(TIME)
	print("Last: " .. SCANJOURNALMESSAGE)
end
