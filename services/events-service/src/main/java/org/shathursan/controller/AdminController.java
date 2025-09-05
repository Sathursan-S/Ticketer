package org.shathursan.controller;

import io.jsonwebtoken.Claims;
import lombok.RequiredArgsConstructor;
import org.shathursan.dto.response.ContentResponse;
import org.shathursan.dto.response.EventResponse;
import org.shathursan.service.EventService;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/v1/admin")
@RequiredArgsConstructor
public class AdminController {

    private final EventService eventService;

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
     * Export booking data (placeholder for CSV/Excel/PDF export)
     */
    @PreAuthorize("hasRole('ADMIN')")
    @GetMapping("/bookings/export")
    public ResponseEntity<ContentResponse<Map<String, String>>> exportBookingData(
            @RequestParam(defaultValue = "csv") String format,
            @RequestParam(required = false) Long eventId) {
        
        try {
            // TODO: Implement actual export functionality
            Map<String, String> exportInfo = new HashMap<>();
            exportInfo.put("format", format);
            exportInfo.put("status", "prepared");
            exportInfo.put("message", "Export functionality is being prepared. Implementation pending for " + format.toUpperCase() + " format.");
            
            if (eventId != null) {
                exportInfo.put("eventId", eventId.toString());
                exportInfo.put("scope", "event-specific");
            } else {
                exportInfo.put("scope", "all-bookings");
            }
            
            ContentResponse<Map<String, String>> response = new ContentResponse<>(
                "Export Data",
                exportInfo,
                "success",
                "200",
                "No Need",
                "Export request processed successfully."
            );
            
            return ResponseEntity.ok(response);
            
        } catch (Exception e) {
            ContentResponse<Map<String, String>> errorResponse = new ContentResponse<>(
                "Export Data",
                null,
                "error",
                "500",
                "No Need",
                "Export failed: " + e.getMessage()
            );
            return ResponseEntity.status(500).body(errorResponse);
        }
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