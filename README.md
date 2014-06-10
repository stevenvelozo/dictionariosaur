Dictionariosaur
===============

A simple test bed for data structures via a tool for working with scrabble word lists and anagrams.

The application runs an asynchronous telnet-style server when you run it, which you can connect to and query for words.

## Building the Application
There are two ways to build the application.  The first is to open the Solution file (Dictionariosaur.sln) in the Source folder with MonoDevelop and click "Build".  This file could potentially work in Visual Studio, but no guarantees.

The second option is to use the NANT build system.  You can just navigate to the root and run "nant" and see something like the following:

	steven at MathBookPro in ~/Dropbox/Code/dictionariosaur on master
	$ nant
	NAnt 0.92 (Build 0.92.4543.0; release; 6/9/2012)
	Copyright (C) 2001-2012 Gerry Shaw
	http://nant.sourceforge.net

	Buildfile: file:///Users/steven/Dropbox/Code/dictionariosaur/default.build
	Target framework: Mono 2.0 Profile
	Target(s) specified: build

	 [property] Target framework changed to "Mono 4.5 Profile".

	build:

	    [mkdir] Creating directory '/Users/steven/Dropbox/Code/dictionariosaur/bin'.
	      [csc] Compiling 9 files to '/Users/steven/Dropbox/Code/dictionariosaur/bin/Dictionariosaur.exe'.

	BUILD SUCCEEDED

	Total time: 0.7 seconds.

	steven at MathBookPro in ~/Dropbox/Code/dictionariosaur on master
	$

## Running the Application
If you are using the fancy MonoDevelop user interface, there is a play button or some similar thing to click on that will execute the application and leave it running.  Otherwise you can do the following from the command-line:

	steven at MathBookPro in ~/Dropbox/Code/dictionariosaur/bin on master*
	$ mono Dictionariosaur.exe
	[12:26:34 PM]Loading and starting the application
	[12:26:34 PM]Starting Telnet Server On Port # 9000
	[12:26:35 PM]Telnet Listening on [MathBookPro.local]10.10.10.46:9000

This is telling us that there is a Telnet server listening on the local machine on port 9000.  Running a firewall?  Open the port to whatever you want to be able to use it or do it by executable.

Now if you want to use the application, run your favorite telnet client and connect to that port.  You should see something like this happen:

	steven at MathBookPro in ~/Dropbox/Code/dictionariosaur/bin on master*
	$ telnet mathbookpro.local 9000
	Trying 10.10.10.46...
	Connected to mathbookpro.local.
	Escape character is '^]'.
	MUTINATION
	----------------------------------------------------| Scrabble Dictionary |----


	[Dictionariosaur]:

### Help
You can access (mostly complete) help by typing in a question mark at the prompt:

	[Dictionariosaur]:?
	?
	Available Commands:
	--------------------------------------------------------------------------=====
	 ADD [some word]       - Add the word [some word] to the list
	 ALPHA [some text]     - Generate an alphagram of [some text]
	 COUNT                 - Show the number of words in the list
	 EXIT                  - Close the telnet session
	 HELP                  - You're lookin' at it
	 INFO                  - Some list statistics
	 LIST [some pattern]   - List words in the list ('LIST ALL' for all words)
	                         Where <some pattern> is a pattern to list
	                         Wildcards are allowed (i.e. '*tion' or 'a*' or '*ire*'
	 LOAD [some file]      - Load a file of CR separated words into the word list
	                         The default file is 'Scrabble.txt'
	 QUIT                  - Close the telnet session and halt the application
	 LOAD [some file]      - Load a file of CR separated words into the word list
	 SEARCH [some word]    - Search the list of words for a word
	 SEARCHALPHA [word]    - Search the list of words for a word by Alphagram
	 WORD [number]         - Display the word at index # [number]
	==========================================================================-----

	[Dictionariosaur]:

### The Word List
At this point the application is waiting for input and has nothing loaded into the dictionary.  You can see that by executing a 'count' command:

	[Dictionariosaur]:count
	count
	There are 0 items in the word list.

	[Dictionariosaur]:

###### Loading Words
Now you will want to load some words into the list, probably.  Unless you like silly empty Telnet shells applications.  The 'load' command does this for you.  You can type it and see what happens:

	[Dictionariosaur]:load
	load
	No File Specified; Using 'Scrabble.txt'
	Loading [Scrabble.txt] into the word list.
	There are now 115288 items in the word list.

	[Dictionariosaur]:

Included is a text file with the OSPD words in it, alphabetized with each word on a single line.  You can technically load any word list you like in.  **If you built the application with MonoDevelop, the content does not automatically copy into the build folder.  You will have to copy the Scrabble.txt file located in the Source/Content folder into the folder with the binary, or wherever you want.**

###### Querying the List
Now for the fun.  You can check to see if a word is in the list with the 'search' command:

	[Dictionariosaur]:search doge
	search doge
	Searching for word [doge] in the word list.
	Found a match!
	 28965. DOGE          Alphagram[DEGO]

	[Dictionariosaur]:search frog
	search frog
	Searching for word [frog] in the word list.
	Found a match!
	 39785. FROG          Alphagram[FGOR]

	[Dictionariosaur]:search frogdoge
	search frogdoge
	Searching for word [frogdoge] in the word list.
	No matches!


	[Dictionariosaur]:

But if you want to really impress your friends, you can search by alphagram:

	[Dictionariosaur]:searchalpha doge
	searchalpha doge
	 Word List:
	--- Number - Word ------------------------------- Alphagram -------------=====
	    24858.   DE                                   DE
	    28839.   DO                                   DO
	    28940.   DOE                                  DEO
	    28954.   DOG                                  DGO
	    28965.   DOGE                                 DEGO
	    31044.   ED                                   DE
	    31291.   EGO                                  EGO
	    41323.   GED                                  DEG
	    42691.   GO                                   GO
	    42756.   GOD                                  DGO
	    67800.   OD                                   DO
	    67818.   ODE                                  DEO
	    67869.   OE                                   EO
	==========================================================================-----
	13 Word(s) Matched the Pattern doge
	Effective Time To Search: 567ms

	[Dictionariosaur]:

And that's about it.  You can connect from multiple telnet sessions at a time and goof off, and change around/fiddle with data structures.  This has been a great test environment for automatically scale testing asynchronous data structures in C#.