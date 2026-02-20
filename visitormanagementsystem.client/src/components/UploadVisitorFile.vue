<template>
  <div class="visitor-upload-container">
    <div class="card shadow-sm p-4">
      <h3 class="mb-4 text-primary">Visitor Management System</h3>
      <p class="text-muted">Upload your visitor records (CSV or Excel format).</p>

      <div class="upload-zone"
           :class="{ 'dragging': isDragging }"
           @dragover.prevent="isDragging = true"
           @dragleave.prevent="isDragging = false"
           @drop.prevent="handleDrop"
           @click="$refs.fileInput.click()">
        <input type="file"
               ref="fileInput"
               hidden
               accept=".csv, .xlsx, .xls"
               @change="handleFileChange" />

        <div class="upload-content text-center">
          <div class="upload-icon mb-3">📁</div>
          <p v-if="!selectedFile">Click to upload or drag and drop files here</p>
          <p v-else class="fw-bold text-success">{{ selectedFile.name }}</p>
          <small class="text-muted">Supported: .csv, .xlsx, .xls</small>
        </div>
      </div>

      <div class="mt-4 d-flex justify-content-end">
        <button class="btn btn-primary"
                :disabled="!selectedFile || isUploading"
                @click="uploadFile">
          {{ isUploading ? 'Processing...' : 'Upload Visitors' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';

const selectedFile = ref(null);
const isDragging = ref(false);
const isUploading = ref(false);

const handleFileChange = (event) => {
  const file = event.target.files[0];
  if (file) selectedFile.value = file;
};

const handleDrop = (event) => {
  isDragging.value = false;
  const file = event.dataTransfer.files[0];
  if (file) selectedFile.value = file;
};

const uploadFile = async () => {
  if (!selectedFile.value) return;

  isUploading.value = true;

  // Preparing FormData for the API
  const formData = new FormData();
  formData.append('visitorFile', selectedFile.value);

  try {
    // Placeholder API endpoint - change this once backend is ready
    console.log("Uploading to backend...", selectedFile.value.name);
    // await api.post('/visitor/upload', formData);

    alert("File uploaded successfully!");
  } catch (error) {
    console.error("Upload failed", error);
    alert("Error uploading file.");
  } finally {
    isUploading.value = false;
  }
};
</script>

<style scoped>
  .visitor-upload-container {
    max-width: 600px;
    margin: 50px auto;
  }

  .upload-zone {
    border: 2px dashed #ccc;
    border-radius: 10px;
    padding: 40px;
    cursor: pointer;
    transition: all 0.3s ease;
    background: #fafafa;
  }

    .upload-zone:hover, .upload-zone.dragging {
      border-color: #0d6efd;
      background: #f0f7ff;
    }

  .upload-icon {
    font-size: 3rem;
  }
</style>
