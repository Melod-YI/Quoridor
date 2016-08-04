package model.service;

import model.state.GameResultState;

public interface StatisticModelService {
	/**
	 * �ڽ�������ʾͳ�ƽ��
	 */
	public void showStatistics();
	
	/**
	 * ��¼��Ϸ��������ڽ�������ʾͳ�ƽ��
	 * @param result ���״̬
	 * @param time ��Ϸʱ��
	 */
	public void recordStatistic(GameResultState result, int time);
}
