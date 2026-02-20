<template>
  <div class="security-wrapper">
    <header class="scanner-header">
      <h1>Gate Access Control</h1>
      <p>Scan visitor or vehicle QR codes</p>
    </header>

    <main class="scanner-container">
      <section class="camera-section" :class="{ 'has-result': result }">
        <div id="reader" class="qr-reader"></div>
        <div v-if="loading" class="scanner-overlay">
          <div class="spinner"></div>
          <p>Verifying Code...</p>
        </div>
        <button v-if="result" @click="resetScanner" class="rescan-btn">
          Scan Another
        </button>
      </section>

      <section v-if="result" class="result-section">
        <div :class="['result-card', result.type === 'Vehicle Pass' ? 'vehicle-theme' : 'visitor-theme']">

          <div class="card-header">
            <span class="type-badge">{{ result.type }}</span>
            <span class="date-badge">{{ formatDate(result.visitDateTime || result.visitDateRaw) }}</span>
          </div>

          <div class="card-content">
            <div class="id-preview">
              <img :src="result.validIdBase64 || result.driverIdBase64" alt="ID Verification" />
              <div class="id-label">OFFICIAL ID ON FILE</div>
            </div>

            <div class="info-grid">
              <div class="info-item full-width">
                <label>Organization / Company</label>
                <p class="val-highlight">{{ result.organization || result.orgName || 'DRIVER' }}</p>
              </div>

              <div class="info-item">
                <label>Branch</label>
                <p>{{ result.branchName }}</p>
              </div>
              <div class="info-item">
                <label>Location</label>
                <p>{{ result.location || 'N/A' }}</p>
              </div>

              <hr class="grid-divider" />

              <template v-if="result.type === 'Visitor Pass'">
                <div class="info-item full-width">
                  <label>Visitor Name</label>
                  <p class="val-lg">{{ result.firstName }} {{ result.middleName }} {{ result.lastName }}</p>
                </div>
                <div class="info-item">
                  <label>Designation</label>
                  <p>{{ result.designation }}</p>
                </div>
                <!--<div class="info-item">
                  <label>Department</label>
                  <p>{{ result.department }}</p>
                </div>-->
              </template>

              <template v-else>
                <div class="info-item full-width">
                  <label>Plate Number</label>
                  <p class="val-lg">{{ result.vehiclePlateNo }}</p>
                </div>
                <div class="info-item">
                  <label>Vehicle Info</label>
                  <p>{{ result.vehicleColor }} {{ result.vehicleModel }}</p>
                </div>
                <div class="info-item">
                  <label>Driver Name</label>
                  <p>{{ result.driverFirstName }} {{ result.driverLastName }}</p>
                </div>
              </template>
            </div>

            <div v-if="result.passengers && result.passengers.length > 0" class="passenger-section">
              <label class="section-label">Approved Passengers ({{ result.passengers.length }})</label>
              <div class="passenger-list-container">
                <div v-for="(p, i) in result.passengers" :key="i" class="passenger-row clickable" @click="viewPassenger(p)">
                  <div class="p-info">
                    <span class="p-name">{{ p.firstName }} {{ p.lastName }} 🔍</span>
                    <span class="p-designation">{{ p.designation }}</span>
                  </div>
                  <span class="p-dept" :title="p.organization || result.organization">
                    {{ p.organization || result.organization }}
                  </span>
                </div>
              </div>
            </div>
          </div>


        </div>

        <div class="card-actions">
          <button class="confirm-btn"
                  @click="confirmEntry"
                  :disabled="loading">
            <span v-if="loading">Processing...</span>
            <span v-else>Confirm Entry</span>
          </button>

          <button @click="resetScanner" class="btn-scan-again" :disabled="loading">
            Scan Another Code
          </button>
        </div>
      </section>

      <Transition name="modal">
        <div v-if="selectedPassenger" class="modal-backdrop" @click.self="closePassengerView">
          <div class="modal-dialog">
            <div class="modal-header">
              <h3>Passenger Verification</h3>
              <button class="close-x" @click="closePassengerView">&times;</button>
            </div>

            <div class="modal-body">
              <div class="id-preview">
                <img :src="selectedPassenger.validIdBase64" alt="Passenger ID" />
                <div class="id-label">OFFICIAL ID ON FILE</div>
              </div>

              <div class="info-grid">
                <div class="info-item full-width">
                  <label>Full Name</label>
                  <p class="val-lg">{{ selectedPassenger.firstName }} {{ selectedPassenger.lastName }}</p>
                </div>
                <div class="info-item">
                  <label>Designation</label>
                  <p>{{ selectedPassenger.designation }}</p>
                </div>
                <div class="info-item">
                  <label>Company / Organization</label>
                  <p class="val-highlight">{{ selectedPassenger.organization || result.organization }}</p>
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button class="modal-close-btn" @click="closePassengerView">Close Profile</button>
            </div>
          </div>
        </div>
      </Transition>
    </main>
  </div>
</template>

<script setup>
  import { ref, onMounted, onUnmounted } from 'vue';
  import { Html5QrcodeScanner } from "html5-qrcode";
  import axios from 'axios';

  const result = ref(null);
  const loading = ref(false);
  let scanner = null;

  // --- HELPER FUNCTIONS ---
  const formatDate = (dateStr) => {
    if (!dateStr) return 'N/A';
    try {
      const date = new Date(dateStr);
      if (isNaN(date.getTime())) return 'Invalid Date';
      return date.toLocaleString('en-US', {
        month: 'short', day: 'numeric', year: 'numeric',
        hour: '2-digit', minute: '2-digit', hour12: true
      });
    } catch (e) {
      return 'N/A';
    }
  };

  const onScanSuccess = async (decodedText) => {
    // CRITICAL: If we are already verifying or showing a result, IGNORE new scans
    if (loading.value || result.value) return;

    loading.value = true;

    try {
      // 1. Pause the camera immediately so it stops looking for codes
      if (scanner) {
        try { await scanner.pause(true); } catch (e) { console.warn("Pause error", e); }
      }

      const isVehicle = decodedText.startsWith("CAR");
      const endpoint = isVehicle
        ? `/api/VisitorMonitoring/retrieve/vehicle/${decodedText}`
        : `/api/VisitorMonitoring/retrieve/visitor/${decodedText}`;

      const res = await axios.get(endpoint);

      if (!res.data) throw new Error("No data received");

      result.value = res.data;
      if (navigator.vibrate) navigator.vibrate(200);

    } catch (err) {
      console.error("Scan Error:", err);
      // Use a small delay for the alert so it doesn't interrupt the UI thread immediately
      setTimeout(() => {
        alert(err.response?.data?.message || "Invalid QR Code");
        result.value = null; // Ensure result is null to show camera again
        resumeScanner();
      }, 100);
    } finally {
      loading.value = false;
    }
  };

  const resumeScanner = () => {
    if (scanner) {
      try {
        scanner.resume();
      } catch (e) {
        // Fallback: If resume fails, re-render
        console.log("Resume failed, restarting scanner...");
        scanner.render(onScanSuccess);
      }
    }
  };

  const resetScanner = () => {
    result.value = null; // Hides the card
    selectedPassenger.value = null; // Closes any open passenger modals
    resumeScanner(); // Restarts the camera feed
  };

  // --- ENTRY CONFIRMATION ---
  const confirmEntry = async () => {
    try {
      loading.value = true;

      // 1. CHANGE "Type" to "type"
      if (result.value.type === "Visitor Pass") {
        // 2. CHANGE "RegistrationCode" to "registrationCode"
        const code = result.value.registrationCode;
        await axios.post('/api/VisitorMonitoring/confirm-entry', { registrationCode: code });
      }
      // 3. CHANGE "Type" to "type" and "Passengers" to "passengers"
      else if (result.value.type === "Vehicle Pass") {
        const passengerCodes = result.value.passengers.map(p => p.registrationCode);
        await Promise.all(passengerCodes.map(code =>
          axios.post('/api/VisitorMonitoring/confirm-entry', { registrationCode: code })
        ));
      }

      alert("Entry confirmed successfully!");
      resetScanner();
    } catch (error) {
      console.error("Confirmation Error:", error);
      alert("Error: " + (error.response?.data?.message || "Confirmation failed."));
    } finally {
      loading.value = false;
    }
  };

  // --- PASSENGER LOGIC ---
  const selectedPassenger = ref(null);
  const viewPassenger = (passenger) => { selectedPassenger.value = passenger; };
  const closePassengerView = () => { selectedPassenger.value = null; };

  // --- LIFECYCLE ---
  onMounted(() => {
    const readerElement = document.getElementById("reader");
    if (!readerElement) return;

    try {
      scanner = new Html5QrcodeScanner("reader", {
        fps: 15,
        qrbox: (vw, vh) => {
          const size = Math.floor(Math.min(vw, vh) * 0.7);
          return { width: size, height: size };
        },
        aspectRatio: 1.0,
        rememberLastUsedCamera: true,
        showTorchButtonIfSupported: true,
        // ADD THIS: Prefer the back camera
        videoConstraints: {
          facingMode: "environment"
        }
      });

      scanner.render(onScanSuccess, () => { });
    } catch (err) {
      console.error("Failed to initialize scanner:", err);
    }
  });

  onUnmounted(async () => {
    if (scanner) {
      try {
        // Html5QrcodeScanner uses clear() to stop the camera and clean the DOM
        await scanner.clear();
      } catch (e) {
        console.warn("Cleanup warning:", e);
      }
    }
  });
</script>

<style scoped>
  .security-wrapper {
    font-family: 'Montserrat', sans-serif;
    background: #1a202c; /* Dark theme for security screens */
    min-height: 100vh;
    color: white;
    padding: 1rem;
  }

  .scanner-header {
    text-align: center;
    margin-bottom: 2rem;
  }

    .scanner-header h1 {
      font-size: 1.5rem;
      margin: 0;
      color: #63b3ed;
    }

  /* Force the camera container to be a square */
  .camera-section {
    width: 100%;
    max-width: 400px;
    aspect-ratio: 1 / 1; /* Key for mobile square layout */
    margin: 0 auto;
    border-radius: 20px;
    overflow: hidden;
    position: relative;
    background: #000;
    border: 4px solid #2d3748;
  }

  /* Ensure the library's internal elements don't break the square */
  #reader {
    width: 100% !important;
    border: none !important;
  }

  /* Force the raw video feed to fill the square container */
  :deep(#reader video) {
    width: 100% !important;
    height: 100% !important;
    object-fit: cover !important; /* Prevents the "stretched" look */
  }

  /* Clean up the library's default UI button spacing for mobile */
  :deep(#reader__dashboard_section_csr button) {
    margin: 10px 5px !important;
    padding: 8px 15px !important;
    border-radius: 8px !important;
    background-color: #4a5568 !important;
    color: white !important;
    border: none !important;
  }

  .qr-reader {
    border: none !important;
  }

  /* Result Card Styling */
  /* Result Section Mobile Adjustments */
  .result-section {
    margin-top: 1.5rem;
    width: 100%;
    max-width: 100%; /* Take full width on small screens */
    padding-bottom: 2rem; /* Extra space for scrolling */
  }

  .result-card {
    background: white;
    border-radius: 16px;
    color: #2d3748;
    overflow: hidden;
    box-shadow: 0 10px 25px rgba(0,0,0,0.3);
  }

  .result-actions {
    display: flex;
    flex-direction: column;
    gap: 12px;
    margin-top: 20px;
    padding: 0 15px;
  }

  .btn-scan-again {
    width: 100%;
    padding: 18px;
    background-color: #f5f5f5; /* Light Grey */
    color: #424242;
    border: none;
    border-radius: 12px;
    font-size: 1.1rem;
    font-weight: 700;
    cursor: pointer;
    transition: all 0.2s;
    font-family: 'Montserrat', sans-serif;
  }

    .btn-scan-again:active {
      background-color: #eeeeee;
      border-color: #bdbdbd;
    }

  .card-header {
    padding: 1rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #edf2f7;
  }

  .visitor-theme .type-badge {
    background: #3182ce;
    color: white;
    padding: 4px 12px;
    border-radius: 20px;
    font-weight: bold;
  }

  .vehicle-theme .type-badge {
    background: #e53e3e;
    color: white;
    padding: 4px 12px;
    border-radius: 20px;
    font-weight: bold;
  }

  .id-preview {
    text-align: center;
    padding: 1rem;
    background: #f7fafc;
  }

    .id-preview img {
      width: 100%;
      max-height: 250px;
      object-fit: contain;
      border-radius: 8px;
    }

  .info-grid {
    padding: 1.5rem;
    display: grid;
    gap: 1rem;
  }

  .info-item label {
    font-size: 0.75rem;
    text-transform: uppercase;
    color: #718096;
    font-weight: bold;
  }

  .val-lg {
    font-size: 1.4rem;
    font-weight: 800;
    color: #1a202c;
    margin: 0;
  }

  .passenger-list {
    padding: 1rem 1.5rem;
    background: #f8fafc;
    border-top: 1px solid #edf2f7;
  }

    .passenger-list ul {
      padding-left: 1.2rem;
      margin-top: 0.5rem;
    }

  .rescan-btn {
    display: block;
    width: 100%;
    padding: 1rem;
    background: #48bb78;
    color: white;
    border: none;
/*    font-weight: bold;*/
    cursor: pointer;
    font-family: 'Montserrat', sans-serif;
  }

  .info-grid {
    padding: 1.5rem;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
  }

  .full-width {
    grid-column: span 2;
  }

  .val-highlight {
    font-size: 1.1rem;
    font-weight: 700;
    color: #003366;
    margin: 0;
  }

  .val-lg {
    font-size: 1.4rem;
    font-weight: 800;
    color: #1a202c;
    line-height: 1.2;
  }

  .grid-divider {
    grid-column: span 2;
    border: 0;
    border-top: 1px solid #e2e8f0;
    margin: 0.5rem 0;
  }

  .passenger-section {
    padding: 1rem 1.5rem;
    background: #f7fafc;
    border-top: 1px solid #edf2f7;
  }

  .passenger-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-top: 0.5rem;
  }

  .p-tag {
    background: #e2e8f0;
    padding: 4px 10px;
    border-radius: 6px;
    font-size: 0.85rem;
    font-weight: 600;
  }

  /* Primary Confirm Button */
  .confirm-btn {
    width: 100%;
    padding: 18px;
    background-color: #2e7d32; /* Rich Green */
    color: white;
    border: none;
    border-radius: 12px;
    font-size: 1.1rem;
    font-weight: 700;
/*    letter-spacing: 1px;*/
    cursor: pointer;
    transition: transform 0.1s, background-color 0.2s;
    display: flex;
    justify-content: center;
    align-items: center;
    font-family: 'Montserrat', sans-serif;
  }

    .confirm-btn:active {
      transform: scale(0.98);
      background-color: #1b5e20;
    }

    .confirm-btn:disabled {
      background-color: #a5d6a7;
      cursor: not-allowed;
    }

  .passenger-section {
    padding: 1.5rem;
    background: #f7fafc;
    border-top: 1px solid #edf2f7;
  }

  .section-label {
    display: block;
    font-size: 0.75rem;
    font-weight: 800;
    color: #718096;
    text-transform: uppercase;
    margin-bottom: 1rem;
    letter-spacing: 0.05em;
  }

  .passenger-list-container {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .passenger-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.5rem 0;
    border-bottom: 1px solid #e2e8f0;
  }

    .passenger-row:last-child {
      border-bottom: none;
    }

  .p-info {
    display: flex;
    flex-direction: column;
  }

  .p-name {
    font-weight: 700;
    color: #2d3748;
    font-size: 0.95rem;
  }

  .p-designation {
    font-size: 0.8rem;
    color: #4a5568;
    font-style: italic;
  }

  /* You can keep the .p-dept class name but it now represents the Company badge */
  .p-dept {
    font-size: 0.75rem;
    background: #ebf8ff; /* Light blue background for company */
    color: #2b6cb0; /* Darker blue text */
    padding: 2px 8px;
    border-radius: 4px;
    font-weight: 600;
    max-width: 150px; /* Added to prevent long company names from breaking layout */
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .passenger-row.clickable {
    cursor: pointer;
    transition: background 0.2s;
    padding: 0.75rem;
    margin: 0 -0.75rem; /* Allow hover to hit edges */
    border-radius: 8px;
  }

    .passenger-row.clickable:hover {
      background: #edf2f7;
    }

  .back-btn {
    background: none;
    border: none;
    color: #3182ce;
    font-weight: 700;
    cursor: pointer;
    font-family: inherit;
  }

  /* Fullscreen overlay for passenger details */
  .passenger-detail-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: #1a202c;
    z-index: 100;
    padding: 1rem;
    overflow-y: auto;
  }

  .p-name {
    display: flex;
    align-items: center;
    gap: 5px;
    font-weight: 700;
    color: #2d3748;
  }

  /* Modal Backdrop - Dims the background */
  .modal-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.8); /* Darker dim */
    backdrop-filter: blur(4px); /* Modern blur effect */
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1000;
    padding: 1rem;
  }

  /* Modal Dialog Box */
  .modal-dialog {
    background: white;
    width: 100%;
    max-width: 450px;
    border-radius: 16px;
    overflow: hidden;
    box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
    color: #2d3748;
    animation: modalSlideUp 0.3s ease-out;
  }

  .modal-header {
    padding: 1rem 1.5rem;
    background: #f8fafc;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid #e2e8f0;
  }

    .modal-header h3 {
      margin: 0;
      font-size: 1.1rem;
      color: #1a202c;
    }

  .close-x {
    background: none;
    border: none;
    font-size: 1.8rem;
    cursor: pointer;
    color: #a0aec0;
    line-height: 1;
  }

  .modal-body {
    max-height: 70vh;
    overflow-y: auto;
  }

  .modal-footer {
    padding: 1rem;
    background: #f8fafc;
    border-top: 1px solid #e2e8f0;
  }

  .modal-close-btn {
    width: 100%;
    padding: 0.8rem;
    background: #4a5568;
    color: white;
    border: none;
    border-radius: 8px;
    font-weight: 700;
    cursor: pointer;
  }

  /* Container for the buttons outside the card */
  .card-actions {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding: 20px;
/*    background: white;*/
/*    border-top: 1px solid #eee;*/
    /* Optional: Make it sticky to the bottom of the screen */
    position: sticky;
    bottom: 0;
    left: 0;
    right: 0;
    box-shadow: 0 -4px 10px rgba(0, 0, 0, 0.05);
    z-index: 100;
  }

  /* Animation for smooth entry */
  @keyframes modalSlideUp {
    from {
      transform: translateY(20px);
      opacity: 0;
    }

    to {
      transform: translateY(0);
      opacity: 1;
    }
  }

  /* Vue Transition Classes */
  .modal-enter-active, .modal-leave-active {
    transition: opacity 0.3s ease;
  }

  .modal-enter-from, .modal-leave-to {
    opacity: 0;
  }

  /* Mobile Info Grid (Stacking on very small screens) */
  @media (max-width: 380px) {
    .info-grid {
      grid-template-columns: 1fr; /* Stack into 1 column for small phones */
    }

    .info-item {
      grid-column: span 1 !important;
    }

    .val-lg {
      font-size: 1.2rem;
    }
  }

  /* Hide library's default UI elements that clutter mobile */
  #reader__dashboard_section_csr button {
    padding: 8px 12px !important;
    text-transform: uppercase;
    font-size: 12px !important;
    font-weight: bold;
  }
</style>
