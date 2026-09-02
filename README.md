# Document Assessment System 3W1P

Sistem ini adalah aplikasi web berbasis ASP.NET Core MVC yang digunakan untuk menilai dokumen proyek berdasarkan framework 3W1P (Winning Concept, Winning Team, Winning System, Performance). Aplikasi ini memungkinkan pengguna mengunggah file PDF, lalu AI Gemini akan membaca dokumen tersebut dan menghasilkan analisis, skor, temuan, serta rekomendasi perbaikan.

## 1. Tujuan Sistem

Tujuan utama dari aplikasi ini adalah:

- Mengunggah dokumen PDF yang berisi rencana atau proposal proyek.
- Menganalisis dokumen menggunakan model AI Gemini.
- Mengecek apakah dokumen sudah sesuai dengan kerangka 3W1P.
- Memberikan skor keseluruhan dan skor per kategori.
- Menampilkan ringkasan, kekuatan, kelemahan, serta rekomendasi perbaikan.
- Membantu pengguna memahami kualitas dokumen secara cepat dan objektif.

## 2. Fitur Utama

- Upload file PDF saja
- Validasi ukuran file maksimal 20 MB
- Validasi jenis file harus PDF
- Integrasi dengan Gemini API
- Menerima file PDF dan mengirimkan isinya ke model AI
- Menampilkan hasil analisis dalam bentuk:
  - skor akhir
  - label skor
  - ringkasan dokumen
  - analisis halaman PDF
  - anomali dan redundansi konten
  - penilaian 3W1P per kategori
  - rekomendasi perbaikan

## 3. Teknologi yang Digunakan

- ASP.NET Core MVC
- C#
- Razor View
- HTML/CSS/JavaScript
- Google Gemini API

## 4. Alur Kerja Aplikasi

Berikut alur sederhana dari sistem ini:

1. Pengguna membuka halaman utama aplikasi.
2. Pengguna memilih file PDF dan mengirimkan form upload.
3. Controller menerima file.
4. Sistem memvalidasi file apakah benar PDF dan tidak terlalu besar.
5. `GeminiService` mengambil file PDF dan mengirimkannya ke endpoint Gemini API.
6. Gemini mengevaluasi dokumen berdasarkan prompt master yang sudah dibuat.
7. Hasil AI diproses ke dalam struktur `AssessmentResult`.
8. Aplikasi menampilkan hasil penilaian ke halaman `Result`.

## 5. Struktur Folder Proyek

Berikut penjelasan struktur folder agar lebih mudah dipahami untuk pemula:

```text
DocumentAssessmentSystem3W1P/
├── Controllers/
│   └── HomeController.cs
│       - Mengatur request dari browser ke halaman utama dan hasil penilaian.
│       - Memvalidasi file PDF yang diupload.
│       - Memanggil service AI untuk analisis.
│
├── Models/
│   └── AssessmentResult.cs
│       - Berisi model data hasil penilaian.
│       - Semua struktur hasil AI disimpan di sini.
│       - Contoh: skor final, detail analisis dokumen, kategori 3W1P.
│
├── Services/
│   └── GeminiService.cs
│       - Tempat logika integrasi ke Gemini API.
│       - Mengirim PDF ke API, menerima jawaban, lalu mengolah JSON hasil.
│       - Menangani retry jika API gagal atau overload.
│
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml
│   │   │   - Halaman utama untuk upload PDF.
│   │   └── Result.cshtml
│   │       - Halaman yang menampilkan hasil penilaian.
│   └── Shared/
│       - Layout umum untuk tampilan web.
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── Images/
│       - File frontend seperti CSS, JavaScript, dan gambar.
│
├── Program.cs
│   - File utama aplikasi ASP.NET Core.
│   - Mendaftarkan MVC dan dependency injection.
│
├── appsettings.json
│   - File konfigurasi aplikasi.
│   - Tempat menyimpan API key Gemini dan model yang dipakai.
│
├── appsettings.Development.json
│   - Konfigurasi khusus saat aplikasi berjalan dalam mode development.
│
├── DocumentAssessmentSystem3W1P.csproj
│   - File proyek .NET yang berisi dependensi dan konfigurasi build.
│
├── README.md
│   - Dokumentasi proyek.
│
└── Properties/
    └── launchSettings.json
        - Pengaturan environment saat menjalankan aplikasi dari VS Code atau IDE.
```

## 6. Penjelasan File Penting

### Program.cs
File ini adalah titik masuk aplikasi. Di sini aplikasi dibuat, MVC di-register, dan `GeminiService` didaftarkan agar bisa dipakai di controller.

### HomeController.cs
Controller ini menangani request dari halaman utama:

- jika file tidak ada, tampilkan error validasi
- jika file bukan PDF, tampilkan error
- jika file terlalu besar, tampilkan error
- jika semua valid, panggil `GeminiService.AnalyzeDocumentAsync(...)`
- hasil analisis dikirim ke view `Result`

### GeminiService.cs
Ini adalah inti dari sistem AI. Tugasnya:

- membaca file PDF yang diupload
- menyiapkan body JSON request ke API Gemini
- mengirim request ke endpoint generative API
- mengecek status response
- mengekstrak hasil teks JSON
- mendeserialisasi hasil ke model `AssessmentResult`
- melakukan retry jika API gagal timeout atau overload

### AssessmentResult.cs
File ini adalah struktur data dari hasil penilaian. Data yang dikembalikan oleh Gemini harus sesuai dengan model ini agar bisa ditampilkan di view.

Contoh hasil yang ada di model:

- DocumentName
- OverallScore
- ScoreLabel
- Summary
- WinningConcept
- WinningTeam
- WinningSystem
- Performance
- DocumentAnalysis

## 7. Cara Menjalankan Aplikasi

Buka terminal di folder proyek lalu jalankan:

```bash
dotnet restore
dotnet build
dotnet run
```

Setelah itu, buka browser dan akses URL yang ditampilkan, biasanya:

```text
https://localhost:5001
```

atau

```text
http://localhost:5000
```

## 8. Cara Mengganti API Key Gemini

Bagian yang harus diganti ada di file [appsettings.json](appsettings.json).

Buka file tersebut, lalu ubah nilai berikut:

```json
{
  "Gemini": {
    "ApiKey": "YOUR API KEY HERE",
    "Model": "gemini-3.6-flash"
  }
}
```

Ganti bagian:

```json
"ApiKey": "YOUR API KEY HERE"
```

dengan API key yang benar dari Google AI Studio atau Gemini API Anda. Contoh:

```json
{
  "Gemini": {
    "ApiKey": "AIzaSy***********************",
    "Model": "gemini-3.6-flash"
  }
}
```

### Langkah-langkah mendapatkan API key:

1. Buka Google AI Studio.
2. Login menggunakan akun Google Anda.
3. Cari menu untuk membuat API key.
4. Copy API key yang muncul.
5. Tempelkan ke `appsettings.json`.
6. Jalankan ulang aplikasi.

> Penting: jangan membagikan API key ke publik atau ke repository yang bisa diakses umum.

### Opsi lain jika tidak ingin ditulis di appsettings.json
Anda juga bisa menggunakan environment variable dengan nama:

```bash
GEMINI_API_KEY
GEMINI_MODEL
```

Sistem ini sudah dibuat agar membaca API key dan model dari environment variable jika konfigurasi di file `appsettings.json` kosong.

## 9. Cara Kerja JSON Response dari Gemini

Aplikasi ini mengharapkan Gemini mengembalikan hasil dalam format JSON. Karena itu, model AI diberi prompt khusus agar menghasilkan struktur yang konsisten.

Bila response tidak sesuai format, sistem akan mencoba membaca string JSON dari hasil Gemini. Jika formatnya salah, aplikasi akan menolak hasilnya dan menampilkan error.

## 10. Penjelasan Skor 3W1P

Aplikasi menilai dokumen dalam beberapa kategori utama:

- Winning Concept: ide atau konsep utama yang ditawarkan.
- Winning Team: kemampuan tim dan struktur organisasi.
- Winning System: sistem, proses, dan operasional yang dibangun.
- Performance: hasil dan indikator performa proyek.

Setiap kategori memiliki:

- skor
- alasan
- checklist
- konsistensi antar langkah
- kelemahan
- rekomendasi

## 11. Catatan Penting untuk Pemula

- Program ini adalah aplikasi web, bukan hanya script Python biasa.
- File upload diproses lewat ASP.NET Core MVC.
- AI tidak membaca dokumen secara langsung dari folder, melainkan dari file yang diupload ke server.
- Semua data hasil analisis harus sesuai dengan kelas `AssessmentResult` agar view dapat menampilkan hasil dengan benar.
- Jika API key belum diisi, aplikasi tidak akan bisa menghubungi Gemini dan akan error.

## 12. Kesimpulan

Sistem ini adalah aplikasi penilaian dokumen berbasis AI yang membantu mengevaluasi dokumen proyek dengan pendekatan 3W1P. Dengan struktur yang sederhana dan arsitektur MVC, proyek ini mudah dikembangkan lebih lanjut untuk fitur seperti:

- upload multi-file
- riwayat hasil analisis
- export laporan PDF
- login user
- database penyimpanan hasil analisis

## 13. Tips Pengembangan Selanjutnya

- Gunakan environment variable untuk keamanan API key.
- Simpan log hasil AI ke database jika ingin audit.
- Tambahkan error handling yang lebih detail.
- Validasi hasil AI secara lebih ketat sebelum ditampilkan ke user.

## 14. Referensi Konfigurasi API Key

Konfigurasi utama ada di [appsettings.json](appsettings.json):

```json
"Gemini": {
  "ApiKey": "YOUR API KEY HERE",
  "Model": "gemini-3.6-flash"
}
```

Ganti nilai `ApiKey` dengan key asli Anda. Jika Anda menggunakan environment variable, pastikan nama variabel sesuai dengan yang dibaca program, yaitu:

```bash
GEMINI_API_KEY
GEMINI_MODEL
```

Dengan begitu, aplikasi bisa berjalan tanpa menuliskan API key secara langsung di file konfigurasi.
