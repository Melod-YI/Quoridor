package model.impl;

import java.util.Observable;

/**
 * ����Model�࣬�̳�Obsevvale����������Observable�ӿ����������ע�ᣬ�������ݱ仯
 * 
 * @author Administrator
 *
 */

public class BaseModel extends Observable {
	/**
	 * ֪ͨ���·�����������������Ҫ֪ͨ�۲��ߵĵط����ô˷���
	 * 
	 * @param data
	 */
	protected void updateChange(UpdateMessage message) {

		super.setChanged();
		super.notifyObservers(message);

	}
}
