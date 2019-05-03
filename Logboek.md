# Logboek om bij te houden wat ik gedaan heb

## 18-03-2019

Overleg gehad over de aanpak waarbij verzonnen is wat het programma moet gaan doen en hoe deze dat voor elkaar zou moeten krijgen op een heel globaal niveau. Hierna ben ik begonnen met het schrijven van een allereerste opzet in Python. Deze versie kan een heel simpel reads file format inlezen en verwerken tot _k_-meren (chunks) en die in een De Bruijn graaf zetten om daarna de De Bruijn graaf af te lopen om de sequentie te vinden. In alles is deze versie minimaal en moet nog veel meer uitbreiding krijgen om niet 100% correcte data ook te kunnen verwerken. Vooral het pad vind algoritme moet op de schop om echt een goede sequentie te kunnen vinden en er moet ingebouwd worden dat de chunks aligned worden ipv dat de De Bruijn graaf of indetieke stukken gebouwd wordt.

## 25-03-2019

Begonnen met een overleg over de stand van zaken om te beslissen waar ik me vandaag op ga focussen. Overleg geweest met andere mensen uit de onderzoeksgroep over het toevoegen van mijn tool aan de repository van de groep. Ik heb de code van vorige week naar C# vertaald. Deze code is uitgewerkt en is meer mogelijkheden voor instellen bijgemaakt (eigen alfabetten bijvoorbeeld). Ik heb meegeluisterd naar het praatje over onderzoek aan de interne klok van rode bloedcellen door een onderzoeksgroeplid. Ik ben toegelaten tot de repository van de onderzoeksgroep om daar mijn code aan toe te voegen. Ik ben begonnen met het documenteren van alle code, min intentie is dit de volgende keer af te maken en daarna bij te houden. Mijn onderzoeksvraag is vastgesteld op "sequence assembly" en dan het schrjven van code ervoo of het verifiëren van code/software paketten en/of het maken van protocollen hiervoor.

Volgende keer: Documentatie afmaken, Samenvoegen van gevonden paden / consesus sequenties, Alfabet nog generieker maken, testdata genereren en kijken waar ik aam toe kom.

## 26-03-2019

Ik heb in de avond de documentatie van de bestaande code afgemaakt zodat ik maandag gewoon aan het werk kan. Mijn doel is vanaf nu deze documenteerstijl vol te houden zodat deze code ook door andere begrepen kan worden en op de lange termijn bruikbaar blijft.

## 01-04-2019

Ik heb 's ochtends twee nieuwe test cases gemaakt een van lengte 200 en een van lengte 1000. Hierna was er overleg over hoe het ervoor stond, hier kwam uit dat het eigenlijk best wel goed ging en de volgende prioriteit was om het padvindalgoritme te verbeteren. Hier ben ik dan ook mee aan de slag gegaan. Het algemene idee is om in de graaf te beginnen en dan vooruit en achteruit (in de sequentie) te lopen en steeds de beste homologie te nemen hierdoor zou het pad vinden een heel stuk preciezer moeten worden en vooral minder sequenties moeten geven en meer lange sequenties waar we echt iets aan hebben. Hierbij moet nog wel nagedacht worden over wat er gebeurt met nodes die twee keer voorkomen in de sequentie en dat soort dingen. Ook ben ik begonnen met meer meten over het algoritme door de looptijd te meten van verschillende onderdelen.

Volgende keer: mappen van read index naar k-meren om het na assembly weer aan de ruwe data te kunnen associëren, vooruitrekenen met homologie van k-meren, padvindalgoritme uitzoeken waarom Visited niet werkt.

## 08-04-2019

Overleg: Mss toch de te kleine reads gebruiken na het assembleren om de sequentie te onderbouwen. Werken aan multifurcaties in het padvinden.

Ik heb me gestort op het verbeteren van het padvind algoritme en de looptijd door de precompute truc die ik de vorige keer verzonnen heb. Het padvind algoritme is al een stuk beter geworden, het levert in testcase 001 het goede pad, in 002 zit een stuk symetrische sequentie waar het algoritme nog niet goed mee overweg kan (tenzij de K groter gekozen wordt dan het symetrische gedeelte). Ik heb aangepast dat Node nu een class is, waardoor het een reference type is, waardoor aanpassingen die ik eraan maak ook daadwerkelijk effect hebben (dat is waarom Visited niet werkte...). Door de precompute truc is de looptijd van 244503 ms waarvan 242699 ms graph linking naar 19978 ms waarvan 18343 ms graph linking voor test case 002, oftewel een versnelling van meer dan 10×! Ik heb een nieuw test case aangemaakt (004) die twee eiwitten door elkaar heeft, deze worden alletwee door het algoritme herkent en beide gegeven (alleen is er op een stuk te weinig overlap om het aan elkaar te plakken). De output sequenties worden nu gefilterd op het zijn van een stricte subsequentie van een grotere sequentie (zowel vooruit als andersom), waardoor alleen de echt verschillende sequenties nog weergegeven worden.

Volgende keer: alle duplicaten uit de (k-1)-meren halen, de gecondenseerde graaf echt maken, associëren van reads met output, en test data genereren.

## 24-04-2019

Begin van het echte project. 's Ochtends begonnen met een planning voor het labwerk en hoe het project gaat lopen. Daarna heb ik wat literatuurstudie gedaan om te kijken wat er te vinden is in dit veld. Hierna hebben we besproken wat de planning is en hoe het project er globaal uit gaat zien. In de middag heb ik het foutje van de duplicate (k-1)-meren uit de code gehaald en gewerkt aan de gecondenseerde graaf. De duplicaten eruit halen maakte dat de code het weer veel beter deed en ook veel sneller. De gecondenseerde graaf begint een beetje te werken, al moet er nog goed onderzocht worden hoe deze zich gedraagt onder uitdagender omstandigheden. Hiervoor heb ik een nieuwe testcase gemaakt 007 deze bevat twee eiwitten met een gelijk begin en einde maar met een stuk niet oerlappend in het midden, dit zou een heel karakterestieke gecondenseerde graaf op moeten leveren, maar deze ben ik nog niet tegengekomen.

Volgende keer (qua code): gecondenseerde graaf maken, associëren reads met output, test data genereren, denken hoe de output visueel gemaakt kan worden.

Volgende keer (vrijdag): protocol doorspreken, basics doornemen

## 26-04-2019

Begin van de dag hebben we het protocol doorgesproken en gekeken wat we eraan wilden veranderen. Hierna heb ik een spreadsheet gemaakt om uit te rekenen hoeveel we van alle stoffen nodig hebben om daar niet meer over na te hoeven denken tijdens het uitvoerwerk. Daarna heb ik Sandcastle geinstaleerd en gebruikt om documentatie van mijn code mee te maken, deze wees mij op nog niet geheel volledige documentatie dus deze heb ik afgemaakt. Deze documentatie is bedoelt voor mij om mij te helpen overzicht te houden en voor mensen die na mij een keer de code willen lezen/begrijpen/gebruiken. 's Middags heb ik de gecondenseerde graaf afgemaakt, nu is de uiteindelijke graaf inderdaad wat ik wilde dat dezezou worden. Ook heb ik gespeeld met het visueel weergeven hiervan hiervoor heb ik gebruik gemaakt van "graphviz" wat een specifiek programeertaaltje heeft voor het weergeven van grafen en hiervoor dus vrij geschikt lijkt.

Volgende keer (qua code): denken hoe de output visueel beter gemaakt kan worden, overlapende stukjes bij de graaf eruit halen, associëren reads met output, test data genereren.

Volgende keer (maandag): eerste praktisch werk, een antilichaam proberen om te kijken hoe en of het allemaal werkt.

## 29-04-2019

De ochtend hebben we de eerste pilot gestart in het lab (zie labjournaal voor meer details). In de middag tijdes de protease stap heb ik doorgewerkt aan de software, hierbij heb ik het programma zo gemaakt dat deze een ander programma (graphviz) aanroept om grafen mooi weer te geven. Het doel is deze te gaan gebruiken in de uiteindelijke output van het programma. Ook ben ik begonnen met het plannen van de uiteindelijke output met welke informatie er nodig is en hoe het eruit moet gaan zien. In de middag is het protocol verder gevolgd waardoor de samples op de huis tuin en keuken massaspectrometer konden aan het eind van de dag, deze data zal tegen dinsdag bekend zijn en zullen we woensdag doornemen. Qua code ben ik begonnen met een html bestand maken met de uiteindelijke report, maar hier is nog veel werk aan.

Volgende keer (qua code): denken hoe de output visueel beter gemaakt kan worden en wat de uiteindelijke output moet worden, associëren reads met output, test data genereren, verzinnen hoe de verificatie te quantificeren.

Volgende keer (woensdag): doornemen data eerste run, kijken of/wanneer het op het andere apparaat kan.

## 01-05-2019

We hebben de resultaten van maandag doorgesproken en het gehad over de algemene theorie van MS/MS etc. Er bleek dat ik een rekenfoutje gemaakt had waardoor de antilichaamconcentratie in het eindmengsel 1 miljoen keer hoger was ingeschat, maar de tweede run die Joost gisteren (30-04-2019) gedaan heeft was wel goed gelukt (200 keer meer antilichaam geinjecteerd door het niet te verdunnen en meer te injecteren). Uit de resultaten bleek dat het mengsel goed genoeg is om op een betere MS te runnen, dus dat gaat hopelijk binnekort lukken. Ook bleek dat alfalytic protease niet gelukt was, we hebben geen idee waarom niet maar gaan het de volgende keer gewoon weer proberen. 's Middags ben ik begonnen aan het report, hiervoor worden nu HTML pagina's gemaakt waar al wat interactiviteit in zit zodat mensen makkelijker kunnen zien wat de output is.

Volgende keer (qua code): alignment reads/contig, load SVG when ready, test data genereren, verzinnen hoe de verificatie te quantificeren, output to JSON.

Volgende keer (vrijdag): algemene theorie fragmentatie doornemen.

## 03-05-2019

We hebben de samples voor de Fusion voorbereid en besproken wat er anders is dan oon de Orbitrap 3. Sinds de vorige keer heeft Joost al de samples op de Fusion geladen, maar de methode was iets te kort ingesteld waardoor er niet genoeg details overbleven om echt goede analyse op te doen, daarom is er nu voor een 2x langere gradient gekozen in het LC systeem. 's Middags heb ik gezocht naar een script voor in silico digest, na veel gepruts wil ik met regex een heel simpele eigen variant schrijven, dat blijkt makkelijker dan andermans code hergebruiken omdat deze vaak veel te veel kan. Ook hebben we het protocol voor as maandag besproken en wijzigingen doorgevoerd van de afgelopen maandag.

Volgende keer (qua code): alignment reads/contig, load SVG when ready, test data genereren, verzinnen hoe de verificatie te quantificeren, output to JSON.

Volgende keer (maandag): praktisch werk

# TODO

## Small things
[ ] Loading the SVG when ready (so waiting when not)
[ ] Build aligments with the reads per contig

## Priority
[ ] Pathfinding
[ ] Unittests

## Later
[x] Homologie
[ ] Alfabet voor massa's (nog meer generieke code)
[ ] Massainformatie ook gebruiken
    Of als er alleen de massa van een dipeptide bekend is
[ ] Output paden samenvoegen
    Consesus sequentie
[ ] Fasta sequenties
[ ] Algoritme verifieren / in kaart brengen
    Testen hoe het algoritme zich gedraagt zodat er bekend is hoeveel data nodig is om het algoritme met een goed resultaat uit te kunnen voeren.

# References

Useful articles to use in the writing of the article.

* De Bruijn graphs introduction: https://www.ncbi.nlm.nih.gov/pmc/articles/PMC5531759/
* Immunoglobulin facts Book: https://books.google.nl/books?id=3GN3V7UJ8isC&lpg=PP1&pg=PP1&redir_esc=y#v=onepage&q&f=false
* Old article about using ms to get protein sequences: https://doi.org/10.1073/pnas.83.17.6233
* Book about protein sequencing protocols: https://www.mobt3ath.com/uplode/book/book-24853.pdf
* Article (2008) about protein sequencing of antibodies: https://doi.org/10.1038/nbt1208-1336
* Article about a piece of learing software for protein sequencing of antibodies: https://doi.org/10.1021/ac048788h
* Article about software used in determining small sequences (peaks):  https://doi.org/10.1002/rcm.1196
   * With useful comparisons to see how that should be done
* Article about own software: https://doi.org/10.1021/ac070039n
   * With useful comparisons to see how that should be done, and with many software products which I should use to compare with
* Article descibing nearly exactly the same algorithm as my software (pTA): https://doi.org/10.1074/mcp.O116.065417
* Article describing a novel way to use de Bruijn graphs to use long error prone reads: https://doi.org/10.1073/pnas.1604560113
* Article describing the general advancements in protein sequencing of the last years: https://doi.org/10.1002/mas.21406