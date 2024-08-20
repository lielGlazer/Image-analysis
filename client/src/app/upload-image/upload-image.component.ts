import { Component } from '@angular/core';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import * as CryptoJS from 'crypto-js';

@Component({
  selector: 'app-upload-image',
  standalone: true,
  imports: [HttpClientModule, CommonModule],
  templateUrl: './upload-image.component.html',
  styleUrls: ['./upload-image.component.css']
})
export class UploadImageComponent {
  selectedFile: File | null = null;
  fileChecksum: string | null = null;
  popularColors: { color: string, percentage: number }[] = [];
  imagePreviewUrl: string | ArrayBuffer | null = null;

  constructor(private http: HttpClient) {}

  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0];
    if (this.selectedFile) {
      this.calculateChecksum(this.selectedFile);
      this.previewImage(this.selectedFile);
    }
  }

  calculateChecksum(file: File) {
    const reader = new FileReader();
    reader.onload = (event: any) => {
      const fileData = event.target.result;
      const wordArray = CryptoJS.lib.WordArray.create(fileData);
      this.fileChecksum = CryptoJS.SHA256(wordArray).toString();
      console.log('Checksum:', this.fileChecksum);
    };
    reader.readAsArrayBuffer(file);
  }

  previewImage(file: File) {
    const reader = new FileReader();
    reader.onload = (event: any) => {
      this.imagePreviewUrl = event.target.result;
    };
    reader.readAsDataURL(file);
  }

  onUpload() {
    if (!this.selectedFile || !this.fileChecksum) {
      window.alert('Please select a file and wait for the checksum calculation.');
      return;
    }

    const chunkSize = 1 * 1024 * 1024;
    let offset = 0;
    const fileSize = this.selectedFile.size;

    const uploadChunk = (start: number, end: number) => {
      if (this.selectedFile) {
        const chunk = this.selectedFile.slice(start, end);
        const formData = new FormData();
        formData.append('file', chunk, this.selectedFile.name!); 
        formData.append('checksum', this.fileChecksum!);

        this.http.post<{ filePath: string, popularColors: string[] }>('https://localhost:7275/upload', formData).subscribe({
          next: (response) => {
            this.popularColors = this.parseColors(response.popularColors);
            window.alert('File uploaded successfully!');
          },
          error: (err) => {
            window.alert('File upload failed. Please try again.');
            console.error('File upload failed:', err);
          }
        });
      }
    };

    
    while (offset < fileSize) {
      const end = Math.min(offset + chunkSize, fileSize);
      uploadChunk(offset, end);
      offset = end;
    }
  }

  parseColors(colors: string[]): { color: string, percentage: number }[] {
    return colors.map(colorString => {
      const [color, percentage] = colorString.split(':');
      return {
        color: color.trim(),
        percentage: parseFloat(percentage.trim().replace('%', ''))
      };
    });
  }
}
