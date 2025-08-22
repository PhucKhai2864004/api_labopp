package Control;

/*
 * Click nbfs://nbhost/SystemFileSystem/Templates/Licenses/license-default.txt to change this license
 * Click nbfs://nbhost/SystemFileSystem/Templates/Classes/Class.java to edit this template
 */
import Common.Constant;
import Model.Course;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Scanner;

/**
 *
 * @author QH
 */
public class CourseManager {

    private ArrayList<Course> lstCourse;

    public CourseManager() {
        lstCourse = new ArrayList<Course>();
    }
    // lấy về danh sách các khóa học có trong hệ thống

    public ArrayList<Course> getCourseList() {
        return new ArrayList<>(lstCourse);
    }
// Trả về khóa học có courseID

    public Course findById(String courseID) {
        Course course;
        courseID = courseID.toLowerCase().trim();
        for (int i = 0; i < lstCourse.size(); i++) {
            course = lstCourse.get(i);
            if (course.getCourseId().toLowerCase().equals(courseID)) {
                return course;
            }
        }
        return null;
    }
//Thêm khóa học

    public void addCourse(Course course) throws Exception {
        if (findById(course.getCourseId()) == null) {
            lstCourse.add(course);
        } else {
            throw new Exception("courseID is existed");
        }
    }
//

    public ArrayList sort() {
        ArrayList<Course> lst = new ArrayList<>(lstCourse);
        Collections.sort(lst);
        return lst;
    }

    public void updateCourse(String courseID, int credit) throws Exception {
        Course course = findById(courseID);
        if (course == null) {
            throw new Exception("Course Id not exist");
        } else {
            course.setCredit(credit);
        }
    }

}
