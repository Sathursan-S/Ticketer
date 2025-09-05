package org.shathursan.controller;

import io.jsonwebtoken.Claims;
import lombok.RequiredArgsConstructor;
import org.shathursan.dto.response.ContentResponse;
import org.shathursan.dto.response.EventResponse;
import org.shathursan.service.EventService;
import org.shathursan.service.ExportService;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.bind.annotation.*;

import java.io.IOException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/api/v1/admin")
@RequiredArgsConstructor
public class AdminController {

    private final EventService eventService;
    private final ExportService exportService;

    /**
     * Get all events with statistics (admin only)
     */
    @PreAuthorize("hasRole('ADMIN')")
    @GetMapping("/events")
    public ResponseEntity<ContentResponse<Page<EventResponse>>> getAllEventsWithStats(
            @RequestParam(required = false) String category,
            @RequestParam(required = false) String location,
            @RequestParam(required = false) String date,
            @RequestParam(required = false) String q,
            Pageable pageable) {
        
        Page<EventResponse> events = eventService.listPublic(category, location, date, q, pageable);
        
        ContentResponse<Page<EventResponse>> response = new ContentResponse<>(
            "Events",
            events,
            "success",
            "200",
            "No Need",
            "Events fetched successfully for admin."
        );
        return ResponseEntity.ok(response);
    }

    /**
     * Get event statistics dashboard (admin only)
     */
    @PreAuthorize("hasRole('ADMIN')")
    @GetMapping("/dashboard/statistics")
    public ResponseEntity<ContentResponse<Map<String, Object>>> getDashboardStatistics() {
        
        List<EventResponse> allEvents = eventService.listAll();
        
        // Calculate statistics
        int totalEvents = allEvents.size();
        long activeEvents = allEvents.stream().mapToLong(e -> "ACTIVE".equals(e.getStatus().toString()) ? 1 : 0).sum();
        long totalTicketsSold = allEvents.stream().mapToLong(EventResponse::getTicketsSold).sum();
        long totalCapacity = allEvents.stream().mapToLong(EventResponse::getTicketCapacity).sum();
        double totalRevenue = allEvents.stream().mapToDouble(e -> e.getTicketsSold() * e.getTicketPrice()).sum();
        
        Map<String, Object> statistics = new HashMap<>();
        statistics.put("totalEvents", totalEvents);
        statistics.put("activeEvents", activeEvents);
        statistics.put("totalTicketsSold", totalTicketsSold);
        statistics.put("totalCapacity", totalCapacity);
        statistics.put("totalRevenue", totalRevenue);
        statistics.put("occupancyRate", totalCapacity > 0 ? (double) totalTicketsSold / totalCapacity * 100 : 0);
        
        ContentResponse<Map<String, Object>> response = new ContentResponse<>(
            "Dashboard Statistics",
            statistics,
            "success",
            "200",
            "No Need",
            "Dashboard statistics fetched successfully."
        );
        
        return ResponseEntity.ok(response);
    }

    /**
     * Get event-specific booking statistics (admin only)
     */
    @PreAuthorize("hasRole('ADMIN')")
    @GetMapping("/events/{eventId}/statistics")
    public ResponseEntity<ContentResponse<Map<String, Object>>> getEventStatistics(@PathVariable Long eventId) {
        
        try {
            EventResponse event = eventService.getEventWithAllStatusById(eventId);
            
            Map<String, Object> eventStats = new HashMap<>();
            eventStats.put("eventId", event.getId());
            eventStats.put("eventName", event.getEventName());
            eventStats.put("status", event.getStatus());
            eventStats.put("ticketCapacity", event.getTicketCapacity());
            eventStats.put("ticketsSold", event.getTicketsSold());
            eventStats.put("ticketsAvailable", event.getTicketsAvailable());
            eventStats.put("ticketPrice", event.getTicketPrice());
            eventStats.put("revenue", event.getTicketsSold() * event.getTicketPrice());
            eventStats.put("occupancyRate", event.getTicketCapacity() > 0 ? 
                (double) event.getTicketsSold() / event.getTicketCapacity() * 100 : 0);
            
            ContentResponse<Map<String, Object>> response = new ContentResponse<>(
                "Event Statistics",
                eventStats,
                "success",
                "200",
                "No Need",
                "Event statistics fetched successfully."
            );
            
            return ResponseEntity.ok(response);
            
        } catch (Exception e) {
            ContentResponse<Map<String, Object>> errorResponse = new ContentResponse<>(
                "Event Statistics",
                null,
                "error",
                "404",
                "No Need",
                "Event not found: " + e.getMessage()
            );
            return ResponseEntity.status(404).body(errorResponse);
        }
    }

    /**
     * Export booking/event data in various formats (CSV, Excel, PDF)
     */
    @PreAuthorize("hasRole('ADMIN')")
    @GetMapping("/events/export")
    public ResponseEntity<byte[]> exportEventData(
            @RequestParam(defaultValue = "csv") String format,
            @RequestParam(required = false) Long eventId) {
        
        try {
            List<EventResponse> events;
            String filename;
            
            if (eventId != null) {
                EventResponse event = eventService.getEventWithAllStatusById(eventId);
                events = List.of(event);
                filename = "event_" + eventId + "_report_" + 
                    LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"));
            } else {
                events = eventService.listAll();
                filename = "all_events_report_" + 
                    LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"));
            }
            
            // Convert events to exportable format
            List<Map<String, Object>> exportData = events.stream()
                .map(this::convertEventToExportMap)
                .collect(Collectors.toList());
            
            String[] headers = {
                "Event ID", "Event Name", "Category", "Status", "Start Date", 
                "Ticket Price", "Capacity", "Sold", "Available", "Revenue",
                "Venue Name", "City", "Country"
            };
            
            byte[] data;
            switch (format.toLowerCase()) {
                case "excel":
                case "xlsx":
                    data = exportService.exportToExcel(exportData, headers);
                    break;
                case "pdf":
                    data = exportService.exportToPDF(exportData, headers);
                    break;
                case "csv":
                default:
                    data = exportService.exportToCSV(exportData, headers);
                    break;
            }
            
            String mimeType = exportService.getMimeType(format);
            String fileExtension = exportService.getFileExtension(format);
            
            return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION, 
                    "attachment; filename=\"" + filename + fileExtension + "\"")
                .header(HttpHeaders.CONTENT_TYPE, mimeType)
                .body(data);
            
        } catch (IOException e) {
            return ResponseEntity.internalServerError().build();
        } catch (Exception e) {
            return ResponseEntity.badRequest().build();
        }
    }
    
    private Map<String, Object> convertEventToExportMap(EventResponse event) {
        Map<String, Object> map = new HashMap<>();
        map.put("Event ID", event.getId());
        map.put("Event Name", event.getEventName());
        map.put("Category", event.getCategory());
        map.put("Status", event.getStatus());
        map.put("Start Date", event.getStartAt());
        map.put("Ticket Price", event.getTicketPrice());
        map.put("Capacity", event.getTicketCapacity());
        map.put("Sold", event.getTicketsSold());
        map.put("Available", event.getTicketsAvailable());
        map.put("Revenue", event.getTicketsSold() * event.getTicketPrice());
        map.put("Venue Name", event.getVenueDto() != null ? event.getVenueDto().getName() : "");
        map.put("City", event.getVenueDto() != null ? event.getVenueDto().getCity() : "");
        map.put("Country", event.getVenueDto() != null ? event.getVenueDto().getCountry() : "");
        return map;
    }

    /**
     * Get admin user details from JWT
     */
    private String getAdminEmail() {
        var authentication = SecurityContextHolder.getContext().getAuthentication();
        if (authentication == null) {
            throw new RuntimeException("Authentication not found");
        }
        Claims claims = (Claims) authentication.getDetails();
        return claims.getSubject();
    }
}