/*
 * Click nbfs://nbhost/SystemFileSystem/Templates/Licenses/license-default.txt to change this license
 * Click nbfs://nbhost/SystemFileSystem/Templates/Classes/Class.java to edit this template
 */

/**
 *
 * @author QH
 */
package Model;

import Common.Constant;

public class Course implements Comparable<Course> {
    private String courseId, courseName;
    private int credit;

    public Course(String courseId, String courseName, int credit) {
        if(courseName.matches(Constant.REGCOURSENAME) 
                &&courseId.matches(Constant.REGCOURSEID)&& credit>=1 && credit<=4)
        {
        this.courseId = courseId;
        this.courseName = courseName;
        this.credit = credit;
        }
    }

    public String getCourseId() {
        return courseId;
    }

    public void setCourseId(String courseId) throws Exception {
        if(courseId.matches(Constant.REGCOURSEID))//[a-zA-Z0-9]+
        this.courseId = courseId;
        else
            throw new Exception("err courseid must be not empty");
    }

    public String getCourseName() {
        return courseName;
    }

    public void setCourseName(String courseName) {
         if(courseName.matches(Constant.REGCOURSENAME))//[a-zA-Z0-9.+]+
        this.courseName = courseName;
    }

    public int getCredit() {
        return credit;
    }

    public void setCredit(int credit) {
        if(credit>=1 && credit<=4)
        this.credit = credit;
    }

    public Course() {
    }

    @Override
    public int compareTo(Course course) {
        if(credit < course.credit)return -1;
        else if(credit == course.credit)return 0;
        else
            return 1;
    }

    @Override
    public String toString() {
        return String.format("%-10s %-20s %d", getCourseId(), getCourseName(), getCredit());
    }

    
    
    
}
