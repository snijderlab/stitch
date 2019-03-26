# Logboek om bij te houden wat ik gedaan heb

## 18-03-2019

Overleg gehad over de aanpak waarbij verzonnen is wat het programma moet gaan doen en hoe deze dat voor elkaar zou moeten krijgen op een heel globaal niveau. Hierna ben ik begonnen met het schrijven van een allereerste opzet in Python. Deze versie kan een heel simpel reads file format inlezen en verwerken tot _k_-meren (chunks) en die in een De Bruijn graaf zetten om daarna de De Bruijn graaf af te lopen om de sequentie te vinden. In alles is deze versie minimaal en moet nog veel meer uitbreiding krijgen om niet 100% correcte data ook te kunnen verwerken. Vooral het pad vind algoritme moet op de schop om echt een goede sequentie te kunnen vinden en er moet ingebouwd worden dat de chunks aligned worden ipv dat de De Bruijn graaf of indetieke stukken gebouwd wordt.

## 25-03-2019

Begonnen met een overleg over de stand van zaken om te beslissen waar ik me vandaag op ga focussen. Overleg geweest met andere mensen uit de onderzoeksgroep over het toevoegen van mijn tool aan de repository van de groep. Ik heb de code van vorige week naar C# vertaald. Deze code is uitgewerkt en is meer mogelijkheden voor instellen bijgemaakt (eigen alfabetten bijvoorbeeld). Ik heb meegeluisterd naar het praatje over onderzoek aan de interne klok van rode bloedcellen door een onderzoeksgroeplid. Ik ben toegelaten tot de repository van de onderzoeksgroep om daar mijn code aan toe te voegen. Ik ben begonnen met het documenteren van alle code, min intentie is dit de volgende keer af te maken en daarna bij te houden. Mijn onderzoeksvraag is vastgesteld op "sequence assembly" en dan het schrjven van code ervoo of het verifiëren van code/software paketten en/of het maken van protocollen hiervoor.

Volgende keer: Documentatie afmaken, Samenvoegen van gevonden paden / consesus sequenties, Alfabet nog generieker maken, testdata genereren en kijken waar ik aam toe kom.

## 26-03-2019

Ik heb in de avond de documentatie van de bestaande code afgemaakt zodat ik maandag gewoon aan het werk kan. Mijn doel is vanaf nu deze documenteerstijl vol te houden zodat deze code ook door andere begrepen kan worden en op de lange termijn bruikbaar blijft.

# TODO

[-] Homologie
[-] Pathfinding
[ ] Unittests
[ ] Alfabet voor massa's (nog meer generieke code)
[ ] Massainformatie ook gebruiken
    Of als er alleen de massa van een dipeptide bekend is
[ ] Output paden samenvoegen
    Consesus sequentie
[ ] Fasta sequenties
[ ] Algoritme verifieren / in kaart brengen
    Testen hoe het algoritme zich gedraagt zodat er bekend is hoeveel data nodig is om het algoritme met een goed resultaat uit te kunnen voeren.

# Referenties

* De Bruijn graphs introduction: https://www.ncbi.nlm.nih.gov/pmc/articles/PMC5531759/
* Immunoglobulin facts Book: https://books.google.nl/books?id=3GN3V7UJ8isC&lpg=PP1&pg=PP1&redir_esc=y#v=onepage&q&f=false