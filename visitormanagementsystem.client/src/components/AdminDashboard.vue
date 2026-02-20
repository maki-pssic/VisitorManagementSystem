<template>
  <div class="admin-wrapper">
    <header class="admin-header">
      <div class="header-left">
        <h1>Visitor Management Admin</h1>
        <p>Manage and monitor visitor and vehicle access</p>
      </div>
      <div class="header-actions">
        <button @click="fetchData" class="refresh-btn">
          <i class="sync-icon">↻</i> Refresh Data
        </button>
      </div>
    </header>

    <main class="admin-container">
      <section class="stats-grid">
        <button @click="activeTab = 'pending'"
                class="stat-card"
                :class="{ 'is-active': activeTab === 'pending' }">
          <span class="stat-label">Pending Approval</span>
          <span class="stat-value">{{ totalPending }}</span>
        </button>

        <button @click="activeTab = 'approved'"
                class="stat-card approved"
                :class="{ 'is-active-approved': activeTab === 'approved' }">
          <span class="stat-label">Approved ({{ selectedDateFormatted }})</span>
          <span class="stat-value">{{ totalApproved }}</span>
        </button>
      </section>

      <div class="tab-navigation">
        <button :class="['tab-btn', { active: activeTab === 'pending' }]"
                @click="activeTab = 'pending'">
          Pending Approvals
        </button>
        <button :class="['tab-btn', { active: activeTab === 'approved' }]"
                @click="activeTab = 'approved'">
          Approved Visitors
        </button>
      </div>

      <div v-if="activeTab === 'pending'" class="tab-content">
        <div class="table-actions">
          <button @click="approveSelected"
                  :disabled="selectedIds.length === 0 || processing"
                  class="approve-btn">
            {{ processing ? 'Processing...' : `Approve Selected (${selectedIds.length})` }}
          </button>
        </div>

        <div class="table-responsive">
          <table class="admin-table">
            <thead>
              <tr>
                <th><input type="checkbox" @change="toggleAll" :checked="isAllSelected" /></th>
                <th>Visitor Name</th>
                <th>Organization</th>
                <th>Visit Date & Time</th>
                <th>Person to Visit</th>
                <th>Branch</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="reg in pendingVisitors" :key="reg.id" @click="viewDetails(reg)" class="clickable-row">
                <td @click.stop><input type="checkbox" :value="reg.id" v-model="selectedIds" /></td>
                <td>
                  <div class="visitor-info">
                    <span class="name">{{ reg.visitorName }}</span>
                    <span class="email">{{ reg.visitorEmail }}</span>
                  </div>
                </td>
                <td>{{ reg.orgName }}</td>
                <td>{{ formatDate(reg.visitDateTime) }}</td>
                <td>{{ reg.personToVisit }}</td>
                <td><span class="badge">{{ reg.branch }}</span></td>
              </tr>

              <tr v-if="!pendingVisitors || pendingVisitors.length === 0">
                <td colspan="6" class="empty-state">No pending registrations found.</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="pagination-footer">
          <button @click="prevPage"
                  :disabled="currentPage === 1"
                  class="pager-btn">
            &larr; Previous
          </button>

          <span class="page-indicator">
            Page <strong>{{ currentPage }}</strong> of {{ totalPages }}
          </span>

          <button @click="nextPage"
                  :disabled="currentPage >= totalPages"
                  class="pager-btn">
            Next &rarr;
          </button>
        </div>
      </div>

      <div v-else class="tab-content">
        <div class="table-actions flex-between">
          <div class="filter-group">
            <label>Filter by Date:</label>
            <input type="date" v-model="filterDate" @change="fetchApproved" />
          </div>
          <button @click="exportToCsv" class="export-btn">
            Export to ZIP
          </button>
        </div>

        <div class="table-responsive">
          <table class="admin-table">
            <thead>
              <tr>
                <th>QR Code</th>
                <th>Visitor Name</th>
                <th>Organization</th>
                <th>Visit Date</th>
                <th>Branch</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="reg in approvedVisitors" :key="reg.id" @click="viewDetails(reg)" class="clickable-row">
                <td class="code-text">{{ reg.registrationCode }}</td>
                <td>{{ reg.visitorName }}</td>
                <td>{{ reg.orgName }}</td>
                <td>{{ formatDate(reg.visitDateTime) }}</td>
                <td>{{ reg.branch }}</td>
                <td><span class="status-pill">Approved</span></td>
              </tr>
              <tr v-if="approvedVisitors.length === 0">
                <td colspan="6" class="empty-state">
                  No approved visitors found for {{ filterDate }}.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </main>
  </div>

  <DialogBox :show="detailsDialog.show" title="Visitor Registration Details" @close="closeDetails">
    <div v-if="detailsDialog.data" class="visitor-details-pass">
      <div class="pass-body">
        <div class="pass-visual">
          <label class="pass-label">VALID ID</label>
          <img :src="detailsDialog.data.visitorIdBase64"
               class="pass-id-image"
               @error="(e) => e.target.src = 'https://via.placeholder.com/150'" />
          <div class="pass-status-tag" :class="activeTab">
            {{ activeTab === 'pending' ? 'FOR APPROVAL' : 'APPROVED' }}
          </div>
        </div>

        <div class="pass-info-grid">
          <div class="info-item">
            <label>Full Name</label>
            <p>{{ detailsDialog.data.visitorName }}</p>
          </div>

          <div class="info-item">
            <label>Organization / Company</label>
            <p class="org-value">{{ detailsDialog.data.orgName }}</p>
          </div>

          <div v-if="detailsDialog.data.driverName !== 'N/A' && detailsDialog.data.driverName !== 'No Vehicle'" class="info-item">
            <label>Vehicle / Driver</label>
            <p>
              <span v-if="detailsDialog.data.plateNumber && detailsDialog.data.plateNumber !== 'N/A'">
                [{{ detailsDialog.data.plateNumber }}]
              </span>
              {{ detailsDialog.data.driverName }}
            </p>
          </div>

          <div class="info-item">
            <label>Visit Date & Time</label>
            <p>{{ formatDate(detailsDialog.data.visitDateTime) }}</p>
          </div>
          <div class="info-item">
            <label>Authorized Personnel</label>
            <p>{{ detailsDialog.data.personToVisit }}</p>
          </div>
          <div class="info-item full-width">
            <label>Purpose of Visit</label>
            <p>{{ detailsDialog.data.purposeOfVisit || 'N/A' }}</p>
          </div>
        </div>
      </div>

      <div class="pass-footer">
        <button @click="closeDetails" class="close-details-btn">Close Details</button>
      </div>
    </div>
  </DialogBox>
</template>

<script setup>
  import { ref, onMounted, computed } from 'vue';
  import axios from 'axios';
  import DialogBox from '@/components/DialogBox.vue'; // Adjust path as needed

  const activeTab = ref('pending');
  const selectedIds = ref([]);
  const processing = ref(false);

  const pendingVisitors = ref([]);
  const approvedVisitors = ref([]);
  const totalPending = ref(0);
  const totalApproved = ref(0);
  const currentPage = ref(1);
  const pageSize = 10;

  const filterDate = ref(new Date().toISOString().split('T')[0]);

  // Computed count for the header stat card
  const approvedTodayCount = computed(() => totalApproved.value);

  const fetchApproved = async () => {
    try {
      // Note: ensure filterDate is already set to today's date (which it is in your ref)
      const res = await axios.get(`/api/VisitorMonitoring/admin/approved-visitors?visitDate=${filterDate.value}`);
      const responseBody = res.data;

      approvedVisitors.value = responseBody.data || [];
      totalApproved.value = responseBody.totalRecords || 0; // This updates the Stat Card
    } catch (err) {
      console.error("Error fetching approved visitors:", err);
      totalApproved.value = 0;
    }
  };

  const totalPages = ref(1); // Add this ref

  const fetchData = async () => {
    try {
      const url = activeTab.value === 'pending'
        ? `/api/VisitorMonitoring/admin/pending-approvals?pageNumber=${currentPage.value}&pageSize=${pageSize}`
        : `/api/VisitorMonitoring/admin/approved-visitors?visitDate=${filterDate.value}&pageNumber=${currentPage.value}&pageSize=${pageSize}`;

      const res = await axios.get(url);
      const body = res.data;

      if (activeTab.value === 'pending') {
        pendingVisitors.value = body.data || [];
        totalPending.value = body.totalRecords || 0;
      } else {
        approvedVisitors.value = body.data || [];
        totalApproved.value = body.totalRecords || 0;
      }

      // Update the total pages from the new API packet
      totalPages.value = body.totalPages || 1;

    } catch (err) {
      console.error("Error fetching data:", err);
    }
  };

  // Add these refs
  const detailsDialog = ref({
    show: false,
    data: null
  });

  // Function to open details
  const viewDetails = (visitor) => {
    detailsDialog.value.data = visitor;
    detailsDialog.value.show = true;
  };

  // Function to close details
  const closeDetails = () => {
    detailsDialog.value.show = false;
    detailsDialog.value.data = null;
  };

  const getFullImageUrl = (path) => {
    if (!path) return 'https://via.placeholder.com/150';

    // 1. Clean up backslashes for web standards
    let cleanPath = path.replace(/\\/g, '/');

    // 2. STRIP 'wwwroot' - This is the most critical part.
    // Static files in wwwroot are served from the root URL.
    // Physical: wwwroot/uploads/visitors/image.jpg 
    // URL: https://localhost:7123/uploads/visitors/image.jpg
    cleanPath = cleanPath.replace(/^wwwroot\//, '').replace(/^\/wwwroot\//, '');

    const API_BASE_URL = 'https://localhost:7123';

    return `${API_BASE_URL}/${cleanPath}`;
  };


  import { watch } from 'vue';
  watch(activeTab, () => {
    currentPage.value = 1;
    fetchData();
  });

  const nextPage = () => {
    if (currentPage.value < totalPages.value) {
      currentPage.value++;
      fetchData();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  const prevPage = () => {
    if (currentPage.value > 1) {
      currentPage.value--;
      fetchData();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  const selectedDate = ref(new Date());

  // Replace your old selectedDateFormatted with this:
  const selectedDateFormatted = computed(() => {
    // Use filterDate.value since that is what the <input type="date"> is bound to
    const date = new Date(filterDate.value);

    // Check for invalid date (happens during typing)
    if (isNaN(date.getTime())) return filterDate.value;

    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  });

  // Add this watcher below your activeTab watcher
  watch(filterDate, async () => {
    await fetchApproved();
    // If we are currently looking at the approved tab, refresh the list too
    if (activeTab.value === 'approved') {
      currentPage.value = 1;
      await fetchData();
    }
  });

  const isAllSelected = computed(() => {
    return pendingVisitors.value.length > 0 && selectedIds.value.length === pendingVisitors.value.length;
  });

  const toggleAll = () => {
    if (isAllSelected.value) {
      selectedIds.value = [];
    } else {
      selectedIds.value = pendingVisitors.value.map(v => v.id);
    }
  };

  const approveSelected = async () => {
    processing.value = true;
    try {
      await axios.patch('/api/VisitorMonitoring/admin/batch-approve', selectedIds.value);
      alert("Visitors approved and emails sent!");
      selectedIds.value = [];
      await fetchData();
    } catch (err) {
      alert("Failed to approve visitors.");
    } finally {
      processing.value = false;
    }
  };

  const exportToCsv = () => {
    const url = `/api/VisitorMonitoring/export-approved-visitors-zip?visitDate=${filterDate.value}`;
    window.location.href = url; // Directly triggers the browser download
  };

  const formatDate = (dateStr) => {
    return new Date(dateStr).toLocaleString([], {
      dateStyle: 'medium',
      timeStyle: 'short'
    });
  };

  onMounted(async () => {
    // 1. Fetch the approved count immediately for the Stat Card
    await fetchApproved();

    // 2. Fetch the data for the current active tab (defaults to pending)
    await fetchData();
  });
</script>

<style scoped>
  @import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@300;400;600;700;800&display=swap');

  .admin-wrapper {
    font-family: 'Montserrat', sans-serif;
    background-color: #f4f7f9;
    min-height: 100dvh;
    padding: 2rem;
    color: #1a202c;
  }

  /* Header Section */
  .admin-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 2rem;
    max-width: 1400px;
    margin-left: auto;
    margin-right: auto;
  }

    .admin-header h1 {
      color: #003366;
      font-weight: 800;
      font-size: 1.8rem;
      margin: 0;
    }

    .admin-header p {
      color: #718096;
      margin: 4px 0 0 0;
    }

  /* Stats Cards */
  .stats-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
    gap: 1.5rem;
    margin-bottom: 2rem;
    max-width: 1400px;
    margin-left: auto;
    margin-right: auto;
  }

  /* Updated .stat-card to handle button defaults */
  .stat-card {
    background: white;
    padding: 1.5rem;
    border-radius: 12px;
    box-shadow: 0 4px 6px rgba(0,0,0,0.05);
    border-top: none; /* Reset button border */
    border-right: none; /* Reset button border */
    border-bottom: none; /* Reset button border */
    border-left: 5px solid #003366;
    display: flex;
    flex-direction: column;
    cursor: pointer; /* Make it look clickable */
    font-family: inherit; /* Inherit Montserrat */
    text-align: left; /* Align text to the left */
    transition: transform 0.1s, box-shadow 0.1s;
  }

    .stat-card:hover {
      background-color: #fcfcfc;
      transform: translateY(-2px);
      box-shadow: 0 6px 12px rgba(0,0,0,0.08);
    }

    .stat-card:active {
      transform: translateY(0);
    }

  /* Active Highlight States */
  .is-active {
    background-color: #f0f7ff !important;
  }

  .is-active-approved {
    background-color: #f0fff4 !important;
  }

  .stat-card.approved {
    border-left-color: #2f855a;
  }

  .stat-label {
    font-size: 0.85rem;
    font-weight: 700;
    color: #718096;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }

  .stat-value {
    font-size: 2rem;
    font-weight: 800;
    color: #2d3748;
    margin-top: 0.5rem;
  }

  /* Tabs */
  .tab-navigation {
    display: flex;
    gap: 10px;
    margin-bottom: -1px; /* Overlap border */
    max-width: 1400px;
    margin-left: auto;
    margin-right: auto;
  }

  .tab-btn {
    padding: 12px 24px;
    border: none;
    background: transparent;
    font-weight: 600;
    color: #718096;
    cursor: pointer;
    border-radius: 8px 8px 0 0;
    transition: all 0.2s;
  }

    .tab-btn.active {
      background: white;
      color: #003366;
      box-shadow: 0 -4px 10px rgba(0,0,0,0.05);
    }

  /* Content Area */
  .admin-container {
    max-width: 1400px;
    margin: 0 auto;
  }

  .tab-content {
    background: white;
    border-radius: 0 12px 12px 12px;
    padding: 1.5rem;
    box-shadow: 0 10px 25px rgba(0,0,0,0.05);
    min-height: 400px;
  }

  /* Table Controls */
  .table-actions {
    display: flex;
    align-items: center;
    gap: 1rem;
    margin-bottom: 1.5rem;
    padding-bottom: 1rem;
    border-bottom: 1px solid #edf2f7;
  }

  .flex-between {
    justify-content: space-between;
  }

  /* Table Design */
  .table-responsive {
    max-height: 400px; /* Adjust this height as needed */
    overflow-y: auto;
    overflow-x: auto;
    border: 1px solid #edf2f7;
    border-radius: 8px;
    margin-bottom: 1rem;
    /* Custom scrollbar for better look */
    scrollbar-width: thin;
    scrollbar-color: #cbd5e0 transparent;
  }

  .admin-table {
    width: 100%;
    border-collapse: collapse;
    text-align: left;
  }

    .admin-table th {
      background: #f8fafc;
      padding: 12px 15px;
      font-size: 0.85rem;
      font-weight: 700;
      color: #4a5568;
      text-transform: uppercase;
      border-bottom: 2px solid #edf2f7;
    }

    .admin-table td {
      padding: 15px;
      border-bottom: 1px solid #edf2f7;
      vertical-align: middle;
    }

    .admin-table tr:hover {
      background-color: #f1f5f9;
    }

    /* Optional: Make the table header stay at the top while scrolling */
    .admin-table thead th {
      position: sticky;
      top: 0;
      z-index: 10;
      background: #f8fafc;
    }

  /* Visitor Info Styling */
  .visitor-info {
    display: flex;
    flex-direction: column;
  }

    .visitor-info .name {
      font-weight: 700;
      color: #2d3748;
    }

    .visitor-info .email {
      font-size: 0.8rem;
      color: #718096;
    }

  /* Badges & Pills */
  .badge {
    background: #e2e8f0;
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 0.75rem;
    font-weight: 600;
  }

  .status-pill {
    background: #c6f6d5;
    color: #22543d;
    padding: 4px 12px;
    border-radius: 999px;
    font-size: 0.75rem;
    font-weight: 700;
  }

  .code-text {
    font-family: monospace;
    font-weight: bold;
    color: #003366;
  }

  /* Buttons */
  .refresh-btn {
    background: white;
    border: 1px solid #cbd5e0;
    padding: 10px 16px;
    border-radius: 8px;
    font-weight: 600;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
  }

  .approve-btn {
    background: #003366;
    color: white;
    border: none;
    padding: 12px 20px;
    border-radius: 8px;
    font-weight: 700;
    cursor: pointer;
  }

    .approve-btn:disabled {
      background: #cbd5e0;
      cursor: not-allowed;
    }

  .export-btn {
    background: #2f855a;
    color: white;
    border: none;
    padding: 10px 18px;
    border-radius: 8px;
    font-weight: 600;
    cursor: pointer;
  }

  /* Ensure the empty state looks good in a fixed height box */
  .empty-state {
    height: 200px;
    vertical-align: middle;
  }

  /* Filter Inputs */
  .filter-group {
    display: flex;
    align-items: center;
    gap: 10px;
    font-size: 0.9rem;
    font-weight: 600;
  }

  input[type="date"] {
    padding: 8px;
    border-radius: 6px;
    border: 1px solid #cbd5e0;
    font-family: 'Montserrat', sans-serif;
  }

  .pagination-footer {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 1.5rem;
    margin-top: 1.5rem;
    padding-top: 1rem;
    border-top: 1px solid #edf2f7;
  }

  .pager-btn {
    padding: 8px 16px;
    background-color: white;
    border: 1px solid #cbd5e0;
    border-radius: 6px;
    font-weight: 600;
    color: #2d3748;
    cursor: pointer;
    transition: all 0.2s;
  }

    .pager-btn:hover:not(:disabled) {
      border-color: #003366;
      color: #003366;
      background-color: #f8fafc;
    }

    .pager-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

  .page-info {
    font-weight: 700;
    font-size: 0.9rem;
    color: #4a5568;
  }

  .clickable-row {
    cursor: pointer;
    transition: background-color 0.2s;
  }

    .clickable-row:hover {
      background-color: #f8fafc !important;
    }

  .visitor-details-pass {
    padding: 10px;
    max-width: 600px;
  }

  .pass-body {
    display: flex;
    gap: 25px;
    margin-bottom: 20px;
  }

  .pass-visual {
    flex: 0 0 180px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }

  .pass-label {
    font-size: 0.7rem;
    font-weight: 800;
    color: #64748b;
    letter-spacing: 1px;
  }

  .pass-id-image {
    width: 100%;
    border-radius: 8px;
    border: 1px solid #e2e8f0;
    box-shadow: 0 2px 4px rgba(0,0,0,0.05);
  }

  .pass-info-grid {
    flex: 1;
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 15px;
  }

  .info-item label {
    display: block;
    font-size: 0.75rem;
    color: #94a3b8;
    margin-bottom: 2px;
  }

  .info-item p {
    font-weight: 600;
    color: #1e293b;
    margin: 0;
  }

  .full-width {
    grid-column: span 2;
  }

  .pass-status-tag {
    text-align: center;
    padding: 6px;
    border-radius: 4px;
    font-size: 0.7rem;
    font-weight: 800;
  }

    .pass-status-tag.pending {
      background: #fef3c7;
      color: #92400e;
    }

    .pass-status-tag.approved {
      background: #dcfce7;
      color: #166534;
    }

  .close-details-btn {
    width: 100%;
    padding: 12px;
    background: #f1f5f9;
    border: none;
    border-radius: 8px;
    font-weight: 600;
    color: #475569;
    cursor: pointer;
  }

  .org-value {
    color: #2c5282; /* Deep blue */
    font-weight: 700;
    text-transform: uppercase;
    font-size: 0.95rem;
  }

  .text-muted {
    color: #a0aec0;
    font-style: italic;
  }

  /* Make the organization column slightly wider for long names */
  .admin-table th:nth-child(3),
  .admin-table td:nth-child(3) {
    min-width: 180px;
  }

  @media (max-width: 500px) {
    .pass-body {
      flex-direction: column;
    }

    .pass-visual {
      flex: 1;
    }
  }

  /* Mobile Tweaks */
  @media (max-width: 768px) {
    .admin-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 1rem;
    }

    .admin-wrapper {
      padding: 1rem;
    }
  }
</style>
