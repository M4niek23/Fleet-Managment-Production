# Fleet Management Production - Instrukcja Uruchomienia

Ten projekt to aplikacja internetowa oparta na frameworku **ASP.NET Core 8.0**, wykorzystująca bazę danych **SQL Server** oraz **Entity Framework Core**.

## 1. Wymagania wstępne

Aby uruchomić projekt, na komputerze muszą być zainstalowane następujące narzędzia:

* **.NET 8.0 SDK**: Projekt celuje w framework `net8.0`.
* **SQL Server** lub **LocalDB**: Aplikacja korzysta z `Microsoft.EntityFrameworkCore.SqlServer`.
* **Visual Studio 2022** (zalecane) lub **Visual Studio Code**.

## 2. Konfiguracja bazy danych

Domyślna konfiguracja wskazuje na użycie lokalnej instancji bazy danych (**LocalDB**).

1. Otwórz plik `appsettings.json`.
2. Znajdziesz tam zdefiniowany `ConnectionString` o nazwie `Default`:
   ```json
   "ConnectionStrings": {
     "Default": "Server=(localdb)\\MSSQLLocalDB;Database=Nowa_Baza;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true"
   }
   ```
3. Jeżeli używasz pełnej wersji SQL Server, zmień ten ciąg znaków na odpowiedni dla Twojej instalacji. Jeżeli uzywasz Visual Studio z domyślnymi ustawieniami, domyślna konfiguracja powinna zadziałać bez zmian.

**Automatyczne tworzenie bazy**
Kod aplikacji zawiera mechanizm SeedService, który przy starcie aplikacji automatycznie próbuje utworzyć bazę danych,
jeżeli ta nie istnieje. Nie musisz ręcznie uruchamiać migracji przy pierwszym uruchomieniu, choć jest to zalecana praktyka.

## 3. Uruchomienie projektu

**Visual Studio**

1. Otwórz plik rozwiązania (jeśli istnieje) lub plik projektu Fleet-Managment-Production.csproj.

2. Poczekaj, aż menedżer pakietów pobierze wymagane zależności (np. Bootstrap, EF Core).

3. Naciśnij przycisk Start (lub klawisz F5).

## 4. Domyślne dane logowania

Po uruchomieniu aplikacji system automatycznie utworzy konto administratora, jeśli jeszcze nie istnieje w bazie danych. Skorzystaj z poniższych danych, aby się zalogować:
 
 1. Admin: admin@fleet.com
 2. Hasło: Admin@123 

Uwaga: Konto posiada przypisaną rolę Admin.

## 5. Dodatkowe informacje

 - Lokalizacja: Aplikajca jest skonfigurowana na polska strefę kulturową (pl-PL), co wpływa na formatowanie liczb i dat.
 - Struktura: Domyślny routing przekierowuje na kontroler Account i akcję Login